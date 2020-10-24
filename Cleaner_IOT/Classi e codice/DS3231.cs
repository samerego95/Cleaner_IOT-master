using System;
using System.Threading.Tasks;
//using Windows.Devices.I2c;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace DS3221_IOT
{
    /// <summary>
    /// DS3231 Raw Data
    /// </summary>
    class DS3231Data
    {
        public int Sec { get; set; }
        public int Min { get; set; }
        public int Hour { get; set; }
        public int Day { get; set; }
        public int Date { get; set; }
        public int Month { get; set; }
        public int Century { get; set; }
        public int Year { get; set; }
    }

    public class DS3231
    {
        public bool initComplete;
        public bool dataDisponibile = false;

        #region Address
        private const byte RTC_I2C_ADDR = 0x68;
        private const byte RTC_SEC_REG_ADDR = 0x00;
        private const byte RTC_MIN_REG_ADDR = 0x01;
        private const byte RTC_HOUR_REG_ADDR = 0x02;
        private const byte RTC_DAY_REG_ADDR = 0x03;
        private const byte RTC_DATE_REG_ADDR = 0x04;
        private const byte RTC_MONTH_REG_ADDR = 0x05;
        private const byte RTC_YEAR_REG_ADDR = 0x06;
        private const byte RTC_TEMP_MSB_REG_ADDR = 0x11;
        private const byte RTC_TEMP_LSB_REG_ADDR = 0x12;
        #endregion

        public static I2cDevice _device;
        public static I2cConnectionSettings _settings;

        /// <summary>
        /// Initialize the sensor
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
#if ARM || ARM64
            var i2CDevices = await DeviceInformation.FindAllAsync(I2cDevice.GetDeviceSelector());

            if (i2CDevices.Count == 0)
            {
                initComplete = false;
                return;
            }

            // 0x40 is the I2C device address
            _settings = new I2cConnectionSettings(RTC_I2C_ADDR);

            //create device
            _device = await I2cDevice.FromIdAsync(i2CDevices[0].Id, _settings);

            initComplete = true;

#else
            initComplete = false;
#endif
        }

        /// <summary>
        /// Read Time from DS3231
        /// </summary>
        /// <returns>DS3231 Time</returns>
        public DateTime ReadTime()
        {
            //if device not init, return empty time
            if (!initComplete)
                return (Convert.ToDateTime("2000/01/01 00:00"));

            byte[] rawData = new byte[7];

            try
            {
                byte[] address = new byte[1];
                address[0] = RTC_SEC_REG_ADDR;

                //indirizzo lettura d'inizio = 0x00, restituisce i dati in rawdata
                _device.WriteRead(address, rawData);

            }
            catch (Exception e)
            {
                Console.WriteLine($"No Device");
                return (Convert.ToDateTime("2000/01/01 00:00"));
            }

            DS3231Data data = new DS3231Data();

            data.Sec = BCD2Int(rawData[0]);
            data.Min = BCD2Int(rawData[1]);
            data.Hour = BCD2Int(rawData[2]);
            data.Day = BCD2Int(rawData[3]);
            data.Date = BCD2Int(rawData[4]);
            data.Month = BCD2Int((byte)(rawData[5] & 0x1F));
            data.Century = rawData[5] >> 7;
            if (data.Century == 1)
                data.Year = 2000 + BCD2Int(rawData[6]);
            else
                data.Year = 1900 + BCD2Int(rawData[6]);

            dataDisponibile = true;

            return new DateTime(data.Year, data.Month, data.Date, data.Hour, data.Min, data.Sec);
        }

        /// <summary>
        /// Set DS3231 Time
        /// </summary>
        /// <param name="time">Time</param>
        public void SetTime(DateTime time)
        {
            byte[] setData = new byte[8];

            setData[0] = RTC_SEC_REG_ADDR;

            setData[1] = Int2BCD(time.Second);
            setData[2] = Int2BCD(time.Minute);
            setData[3] = Int2BCD(time.Hour);
            setData[4] = Int2BCD(((int)time.DayOfWeek + 7) % 7);
            setData[5] = Int2BCD(time.Day);
            if (time.Year >= 2000)
            {
                setData[6] = (byte)(Int2BCD(time.Month) + 0x80);
                setData[7] = Int2BCD(time.Year - 2000);
            }
            else
            {
                setData[6] = Int2BCD(time.Month);
                setData[7] = Int2BCD(time.Year - 1900);
            }

            //scrive buffer tempo già formattato correttamente
            _device.Write(setData);
        }

        /// <summary>
        /// Read DS3231 Temperature
        /// </summary>
        /// <returns></returns>
        public double ReadTemperature()
        {
            byte[] address = { RTC_TEMP_MSB_REG_ADDR };
            byte[] data = new byte[2];

            //set indirizzo sensore temperatura
            _device.WriteRead(address, data);

            return data[0] + (data[1] >> 6) * 0.25;
        }

        /// <summary>
        /// BCD To Int
        /// </summary>
        /// <param name="bcd"></param>
        /// <returns></returns>
        private int BCD2Int(byte bcd)
        {
            return ((bcd / 16 * 10) + (bcd % 16));
        }

        /// <summary>
        /// Int To BCD
        /// </summary>
        /// <param name="dec"></param>
        /// <returns></returns>
        private byte Int2BCD(int dec)
        {
            return (byte)((dec / 10 * 16) + (dec % 10));
        }
    }
}