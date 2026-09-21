using System.Windows;
using System.Windows.Data;
using System.Xml.Linq;
using Wpf.Ui.Controls;

namespace BindingTests;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Exercise the shipped controls and binding declarations without starting the taskbar engine.
        var page = XDocument.Load(args[0]);
        var model = new InputModel();
        int checks = 0;
        foreach (var element in page.Descendants().Where(e => e.Name.LocalName == "NumberBox"))
        {
            string declaration = element.Attribute("Value")!.Value;
            string path = declaration.Split(',')[0].Replace("{Binding ", "").Trim();
            var binding = new Binding(path) { Source = model, Mode = BindingMode.TwoWay };
            if (declaration.Contains("UpdateSourceTrigger=PropertyChanged"))
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            var input = new NumberBox { Minimum = -100, Maximum = 100 };
            BindingOperations.SetBinding(input, NumberBox.ValueProperty, binding);
            // Reproduce a value change while the editor remains focused: Apply must not read the old model.
            foreach (double value in new[] { 4d, 12d, 3d })
            {
                input.SetCurrentValue(NumberBox.ValueProperty, (double?)value);
                object owner = model;
                string property = path;
                if (path.StartsWith("Settings.")) { owner = model.Settings; property = path[9..]; }
                double actual = Convert.ToDouble(owner.GetType().GetProperty(property)!.GetValue(owner));
                if (actual != value) throw new InvalidOperationException($"{path}: control={value}, model={actual}");
                checks++;
            }
        }
        Console.WriteLine($"PASS: {checks} immediate NumberBox-to-settings updates across all six inputs.");
        return 0;
    }
}

public sealed class InputModel
{
    public double MarginValue { get; set; } = 3;
    public double Radius { get; set; } = 7;
    public FluentTB.Types.Settings Settings { get; } = new() { MarginTop = 3, MarginBottom = 3, MarginLeft = 3, MarginRight = 3 };
}
