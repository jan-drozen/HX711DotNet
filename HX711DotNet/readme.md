# HX711DotNet

This is a .NET Standard translation of the Python project [hx711py](https://github.com/tatobari/hx711py).
It targets .NET Standard 2.1 and it utilizes the [RaspberryIO](https://github.com/unosquare/raspberryio) for the
low-level stuff.

## Examples
There is the TestApplication project that contains some basic examples. The class Example is again C# translation
of the original example.py. You can simply run by

```c#
var example = new Example(dout, pd_sck);
example.Run();
```

### Note
Do not forget to call `Pi.Init<BootstrapWiringPi>()` (or different wiring) in your application before using the stuff.