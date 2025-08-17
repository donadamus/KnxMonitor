using KnxModel;
using System;

class Program
{
    static void Main(string[] args)
    {
        // Test different float values
        var testValues = new float[] { 0.0f, 1.0f, 50.0f, 100.0f };
        
        foreach (var value in testValues)
        {
            var knxValue = new KnxValue(value);
            var typedValue = knxValue.GetTypedValue();
            
            Console.WriteLine($"Input: {value}f");
            Console.WriteLine($"RawData: [{string.Join(", ", knxValue.RawData)}]");
            Console.WriteLine($"DataLength: {knxValue.DataLength}");
            Console.WriteLine($"TypedValue: {typedValue} (Type: {typedValue.GetType().Name})");
            Console.WriteLine($"ToString(): {knxValue.ToString()}");
            Console.WriteLine();
        }
    }
}
