namespace TeaDriven.StatusPotentiae

module Logger =
    open System.Diagnostics
    open System.Security

    let [<Literal>] log = "Application"
    let [<Literal>] source = "Status Potentiae"

    let eventLogInitialized =
        try
            if not <| EventLog.SourceExists source
            then EventLog.CreateEventSource(source, log)

            true
        with
        | :? SecurityException -> false

    let private writeEntry entryType message =
        if eventLogInitialized
        then EventLog.WriteEntry(source, message, entryType)

    let info message = writeEntry EventLogEntryType.Information message
    let warn message = writeEntry EventLogEntryType.Warning message
    let error message = writeEntry EventLogEntryType.Error message

module SingleInstance =
    open System.Threading
    open System.Reflection

    let mutable mutex = Unchecked.defaultof<Mutex>

    let start () =
        let mutable onlyInstance = false

        // Note: using local mutex, so multiple instantiations are still 
        // possible across different sessions.
        let mutexName =
            sprintf "Local\\%s" (Assembly.GetExecutingAssembly().GetName().Name)

        mutex <- new Mutex(true, mutexName, &onlyInstance)
        onlyInstance

    let stop () = mutex.ReleaseMutex()

module Autostart =
    open System
    open System.IO
    open System.Reflection
    open System.Runtime.InteropServices

    let private createShortcut executablePath shortcutPath =
        let shellType = Guid "72C24DD5-D70A-438B-8A42-98424B88AFB8" |> Type.GetTypeFromCLSID
        let shell = Activator.CreateInstance shellType

        try
            let link =
                shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, [| shortcutPath |])
    
            try
                shellType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, [| executablePath |]) |> ignore
                shellType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, [| sprintf "%s, 0" executablePath |]) |> ignore
                shellType.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null) |> ignore
            finally
                Marshal.FinalReleaseComObject link |> ignore
        finally
            Marshal.FinalReleaseComObject shell |> ignore

    let createAutostartShortcut title =
        let executablePath = Assembly.GetExecutingAssembly().Location
        let shortcutName = sprintf "%s.lnk" title

        let shortcutPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), shortcutName)

        if not <| File.Exists shortcutPath
        then createShortcut executablePath shortcutPath
        
