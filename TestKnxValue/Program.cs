using System;
using KnxModel;

class Program
{
    static void Main()
    {
        Console.WriteLine("Testing KnxValue with float 0.0:");
        
        var knxValue = new KnxValue(0.0f);
        Console.WriteLine($"Raw data bytes: [{string.Join(", ", knxValue.RawData)}]");
        Console.WriteLine($"Data length: {knxValue.DataLength}");
        Console.WriteLine($"Raw value type: {knxValue.RawValue?.GetType()?.Name ?? "null"}");
        Console.WriteLine($"Raw value: {knxValue.RawValue}");
        Console.WriteLine($"AsBoolean(): {knxValue.AsBoolean()}");
        Console.WriteLine($"AsPercentageValue(): {knxValue.AsPercentageValue()}");
        Console.WriteLine($"ToString(): {knxValue}");
        
        Console.WriteLine("\nTesting with float 50.0:");
        var knxValue50 = new KnxValue(50.0f);
        Console.WriteLine($"Raw value: {knxValue50.RawValue}");
        Console.WriteLine($"AsPercentageValue(): {knxValue50.AsPercentageValue()}");
        
        Console.WriteLine("\nTesting with float 0.0 directly as percentage:");
        var percentValue = knxValue.AsPercentageValue();
        Console.WriteLine($"0.0f -> AsPercentageValue(): {percentValue}");
        
        if (percentValue == 0.0f)
        {
            Console.WriteLine("✅ KnxValue(0.0f) correctly returns 0.0f as percentage!");
        }
        else
        {
            Console.WriteLine($"❌ KnxValue(0.0f) returns {percentValue} instead of 0.0f");
        }
    }
}
