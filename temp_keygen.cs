using WebPush;
var keys = VapidHelper.GenerateVapidKeys();
Console.WriteLine($""PUBLIC:{keys.PublicKey}"");
Console.WriteLine($""PRIVATE:{keys.PrivateKey}"");
