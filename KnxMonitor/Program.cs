using KnxService;

Console.WriteLine("KNX Bus Monitor Start...");

//var iotparams = new IoTConnectorParameters()
//{
//    KnxAddress = new Knx.Falcon.IndividualAddress("1.1.16")
//};


//var bus2 = new KnxBus(iotparams);

//bus2.Connect();

//Console.WriteLine(bus2.ConnectionState.ToString());

// Removed the following line because 'KnxService' is a namespace, not a type:
// var service = new KnxService();
var service = new KnxService.KnxService();


service.GroupMessageReceived += (sender, args) =>
{
    Console.WriteLine($"Received Group Address: {args.Destination}, Value: {args.Value}");
};

service.WriteGroupValue("1", "1", "14", true); // Example usage

//var device = bus.OpenConnection(new Knx.Falcon.IndividualAddress("1.1.16"));

//var a = device.DeviceDescriptor0;


//bus.WriteGroupValue(new Knx.Falcon.GroupAddress("1/1/14"), new Knx.Falcon.GroupValue(true),Knx.Falcon.MessagePriority.System );

// Tworzymy połączenie
Console.WriteLine("Połączono. Naciśnij dowolny klawisz, aby zakończyć...");
    Console.ReadKey();
service.Dispose();

