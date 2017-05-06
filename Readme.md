# Important Note

I did not create this. This is an almost exact reimplementation of [Power Plan Switcher](https://github.com/andy722/power-plan-switcher), which is assumed to be unmaintained and (per [this issue](https://github.com/andy722/power-plan-switcher/issues/1)) in the public domain, with some additional enhancements. All the difficult work in figuring out the power management API calls has been done by the original author.

# What is it?

**Status Potentiae** is a simple Windows tray application that detects changes to the machine's AC connection status and changes the current power plan to a pre-selected value. It also displays the connection state and battery level in its tray icon and allows setting the current power plan explicitly as well as opening the "Power Options" window.

# Why is it?

Windows allows you to choose certain settings (e.g. time to turn off display/go to sleep) for a power plan for the AC connected and disconnected states respectively. However, the base power consumption already varies greatly between power plans (battery life can be nearly halved for the "High performance" plan as opposed to "Balanced", even if no applications actually use the additional performance), so it may be preferable to switch plans outright when AC power is connected or disconnected.

# Enhancements over the original

The original application always switched to "High performance" when AC power was connected and to "Power saver" when disconnected. **Status Potentiae** allows choosing the respective power plans to switch to individually.

# Limitations

- Only the predefined power plans "High performance", "Balanced" and "Power saver" are currently supported. Behavior with custom defined power plans is currently untested and undefined.
- AC connection changes while in STR/STD are currently not detected; i.e. putting the computer to sleep on AC power and waking it up on battery will not trigger the correct power plan change.

# Technical information

**Status Potentiae** is a .NET application written in F#. The minimum required .NET version is 4.5.2 (the currently oldest .NET 4.x version still in active support by Microsoft). The application has only been tested on Windows 10 but can reasonably be expected to work from Windows 7 upwards.

## Building

Clone the repository, run `build.cmd`. You may get an error about `C:\Program Files (x86)\Windows Kits\10\bin\x64\rc.exe` being missing (even on Windows 10); in that case change the respective line in the pre-build event of the project to say `C:\Program Files (x86)\Windows Kits\8.1\bin\x64\rc.exe" $(ProjectDir)resources.rc`.