using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace GoProWiFiEnabler
{
    internal class Program
    {
        static DeviceInformation BLEDevice = null;
        static GattCharacteristic sendCommands = null;
        static GattCharacteristic APNameCharacteristic = null;
        static GattCharacteristic APPasswordCharacteristic = null;

        public static string s_readAPName = "b5f90002-aa8d-11e3-9046-0002a5d5c51b";
        public static string s_readAPPass = "b5f90003-aa8d-11e3-9046-0002a5d5c51b";
        public static string s_sendCommands = "b5f90072-aa8d-11e3-9046-0002a5d5c51b";

        public static IReadOnlyList<GattDeviceService> servicesList = null;

        static async Task Main(string[] args)
        {
            // Specify Bluetooth LE Protocol as per here:
            // https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/aep-service-class-ids

            string BLESelector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";
            DeviceInformationKind deviceInformationKind = DeviceInformationKind.AssociationEndpoint;
            string[] requiredProperties = { "System.Devices.Aep.Bluetooth.Le.IsConnectable", "System.Devices.Aep.IsConnected" };
            DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(BLESelector, requiredProperties, deviceInformationKind);

            // Register event handlers before starting the watcher.
            // Added, Updated and Removed are required to get all nearby devices.
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            // EnumerationCompleted and Stopped are optional to implement.
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            Console.WriteLine("Watching for Bluetooth devices...");
            // Start the watcher.
            deviceWatcher.Start();

            while (true)
            {
                Boolean bWiFiAPOn = false;

                // If we haven't seen our device yet, keep waiting.
                if (BLEDevice == null)
                {
                    Thread.Sleep(200);
                }
                else
                {
                    Console.WriteLine("Pairing with GoPro...");
                    BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(BLEDevice.Id);
                    bluetoothLeDevice.DeviceInformation.Pairing.Custom.PairingRequested += Custom_PairingRequested;

                    if (bluetoothLeDevice.DeviceInformation.Pairing.CanPair)
                    {
                        DevicePairingProtectionLevel dppl = bluetoothLeDevice.DeviceInformation.Pairing.ProtectionLevel;
                        DevicePairingResult dpr = await bluetoothLeDevice.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ConfirmOnly, dppl);

                        if (dpr.Status != DevicePairingResultStatus.Paired)
                        {
                            Console.WriteLine("Pairing result: " + dpr.Status.ToString());
                            Console.WriteLine("Press any key to exit...");
                            Console.ReadKey();
                            break;
                        }
                        Console.WriteLine("Successfully paired.");
                    }
                    else
                    {
                        Console.WriteLine("Device can not be paired.");
                        if (bluetoothLeDevice.DeviceInformation.Pairing.IsPaired)
                        {
                            Console.WriteLine("Device is already paired.");
                        }                        
                    }

                    // Get all of the services.
                    Console.WriteLine("Getting services...");
                    GattDeviceServicesResult getServicesResult = await bluetoothLeDevice.GetGattServicesAsync();

                    if (getServicesResult.Status == GattCommunicationStatus.Success)
                    {
                        // As we get each service store them.
                        servicesList = getServicesResult.Services;                                              
                    }

                    foreach (var service in servicesList)
                    {
                        Console.WriteLine("Service UUID: " + service.Uuid);
                        GattCharacteristicsResult characteristicsResult = await service.GetCharacteristicsAsync();

                        if (characteristicsResult.Status == GattCommunicationStatus.Success)
                        {
                            var characteristics = characteristicsResult.Characteristics;
                            foreach (var characteristic in characteristics)
                            {
                                Console.WriteLine("Found characteristic: " + characteristic.Uuid.ToString());

                                // Store the characteristics we use later.
                                // For sending commands (to enable WiFi AP).
                                if (characteristic.Uuid.ToString() == s_sendCommands)
                                {
                                    sendCommands = characteristic;
                                }
                                // AP name.
                                if (characteristic.Uuid.ToString() == s_readAPName)
                                {
                                    APNameCharacteristic = characteristic;
                                }
                                // Ap password.
                                if (characteristic.Uuid.ToString() == s_readAPPass)
                                {
                                    APPasswordCharacteristic = characteristic;
                                }

                            }
                        }
                        else
                        {
                            Console.WriteLine("Failed to get characteristics: " + characteristicsResult.Status.ToString());
                        }
                    }

                    // Get the AP name by reading the characteristics properties.
                    if (APNameCharacteristic != null)
                    {
                        GattCharacteristicProperties properties = APNameCharacteristic.CharacteristicProperties;
                        if (properties.HasFlag(GattCharacteristicProperties.Read))
                        {
                            // This characteristic supports reading from it.
                            GattReadResult readResult = await APNameCharacteristic.ReadValueAsync();
                            if (readResult.Status == GattCommunicationStatus.Success)
                            {
                                DataReader dataReader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value);
                                string data = dataReader.ReadString(readResult.Value.Length);
                                Console.WriteLine("AP Name: " + data);
                            }
                        }
                    }

                    // Get the AP password by reading the characteristics properties.
                    if (APPasswordCharacteristic != null)
                    {
                        GattCharacteristicProperties properties = APPasswordCharacteristic.CharacteristicProperties;
                        if (properties.HasFlag(GattCharacteristicProperties.Read))
                        {
                            // This characteristic supports reading from it.
                            GattReadResult readResult = await APPasswordCharacteristic.ReadValueAsync();
                            if (readResult.Status == GattCommunicationStatus.Success)
                            {
                                DataReader dataReader = Windows.Storage.Streams.DataReader.FromBuffer(readResult.Value);
                                string data = dataReader.ReadString(readResult.Value.Length);
                                Console.WriteLine("AP Password: " + data);
                            }
                        }
                    }

                    // Turn on WiFi.
                    GattCommunicationStatus sendCommandResult = GattCommunicationStatus.Unreachable;
                    if (sendCommands != null && !bWiFiAPOn)
                    {
                        Console.WriteLine("Turning on WiFi access point...");
                        DataWriter mm = new DataWriter();
                        mm.WriteBytes(new byte[] { 0x03, 0x17, 0x01, 0x01 });
                        sendCommandResult = await sendCommands.WriteValueAsync(mm.DetachBuffer());

                        if (sendCommandResult == GattCommunicationStatus.Success)
                        {
                            Console.WriteLine("WiFi AP successfully enabled.");
                            bWiFiAPOn = true;
                        }
                        else
                        {
                            Console.WriteLine("Failed to send command: " + sendCommandResult.ToString());
                        }
                    }                                        
                    else
                    {               
                         Console.WriteLine("Failed to get services: " + getServicesResult.Status.ToString());
                    }

                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    break;
                }
            }
        }

        private static void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            //throw new NotImplementedException();
        }

        private static void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // We only care about the GoPro device.
        }

        private static void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // We only care about the GoPro device.
        }

        private static void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (!string.IsNullOrWhiteSpace(args.Name))
            {
                Console.WriteLine(args.Name);
                // If the device is a GoPro we set the device to it.                
                if (args.Name.Contains("GoPro"))
                {
                    BLEDevice = args;
                }
            }
        }

        private static void Custom_PairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            // On initial pairing we much accept the request.
            Console.WriteLine("Confirming pairing request...");
            args.Accept();
        }
    }
}
