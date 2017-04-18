namespace TeaDriven.StatusPotentiae

//[<RequireQualifiedAccess>]
module PowerManagement =
    open System
    open System.Diagnostics
    open System.IO
    open System.Runtime.InteropServices
    open System.Windows.Forms

    type PowerPlan = { Name : string; Guid : Guid }

    module Imports =
        [<DllImport("kernel32.dll")>]
        extern int GetSystemDefaultLCID()

        [<DllImport("powrprof.dll", EntryPoint = "PowerSetActiveScheme")>]
        extern uint32 PowerSetActiveScheme(IntPtr UserPowerKey, Guid& ActivePolicyGuid)

        [<DllImportAttribute("powrprof.dll", EntryPoint = "PowerGetActiveScheme")>]
        extern uint32 PowerGetActiveScheme(IntPtr UserPowerKey, IntPtr& ActivePolicyGuid)

        [<DllImportAttribute("powrprof.dll", EntryPoint = "PowerReadFriendlyName")>]
        extern uint32 PowerReadFriendlyName(IntPtr RootPowerKey, Guid& SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr Buffer, uint32& BufferSize)

    let getPowerPlanName guid =
        let mutable guid = guid
        let mutable name = ""
        let mutable lpszName : IntPtr = Unchecked.defaultof<IntPtr>
        let mutable dwSize = 0u

        Imports.PowerReadFriendlyName(Unchecked.defaultof<IntPtr>, &guid, Unchecked.defaultof<IntPtr>, Unchecked.defaultof<IntPtr>, lpszName, &dwSize) |> ignore

        if dwSize > 0u
        then
            lpszName <- Marshal.AllocHGlobal(int dwSize)

            if 0u = Imports.PowerReadFriendlyName(Unchecked.defaultof<IntPtr>, &guid, Unchecked.defaultof<IntPtr>, Unchecked.defaultof<IntPtr>, lpszName, &dwSize)
            then
                name <- Marshal.PtrToStringUni lpszName

            if lpszName <> IntPtr.Zero then Marshal.FreeHGlobal lpszName

        name

    let getActiveGuid () =
        let mutable activeScheme = Guid.Empty
        let mutable ptr = Marshal.SizeOf typeof<IntPtr> |> Marshal.AllocHGlobal

        if Imports.PowerGetActiveScheme(Unchecked.defaultof<IntPtr>, &ptr) = 0u
        then
            activeScheme <- Marshal.PtrToStructure(ptr, typeof<Guid>) :?> Guid

            if ptr <> IntPtr.Zero then Marshal.FreeHGlobal ptr
    
        activeScheme

    let openPowerOptions () =
        let root = Environment.GetEnvironmentVariable "SystemRoot"
        
        Process.Start(Path.Combine(root, "system32", "control.exe"), "/name Microsoft.PowerOptions")
        |> ignore

    let isCharging () =
        SystemInformation.PowerStatus.PowerLineStatus = PowerLineStatus.Online

    let getChargeValue () =
        SystemInformation.PowerStatus.BatteryLifePercent * 100.f |> int

    let private newPlan (planId : string) =
        let guid = Guid planId
        { Name = getPowerPlanName guid; Guid = guid }

    let maximumPerformancePlan = newPlan "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"
    let balancedPlan = newPlan "381b4222-f694-41f0-9685-ff5bb260df2e"
    let powerSourceOptimizedPlan = newPlan "a1841308-3541-4fab-bc81-f71556f20b4a"

    let getPlans () = [ maximumPerformancePlan; balancedPlan; powerSourceOptimizedPlan ]

    let setActive uiCallback plan =
        let mutable guid = plan.Guid

        Imports.PowerSetActiveScheme(IntPtr.Zero, &guid) |> ignore
        uiCallback()

        sprintf "Switched to %s" plan.Name
        |> Logger.info

    let getActivePlan () =
        let activeGuid = getActiveGuid ()

        getPlans ()
        |> List.tryFind (fun plan -> plan.Guid = activeGuid)
        |> Option.defaultValue { Name = "Unknown plan"; Guid = activeGuid }

    
