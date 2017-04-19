namespace TeaDriven.StatusPotentiae

module Program =
    open System.Windows.Forms

    open ApplicationContext

    [<EntryPoint>]
    let main argv =
        if SingleInstance.start ()
        then
            // TODO Add Autostart handling

            try
                new TrayApplicationContext()
                |> Application.Run
            with
            | ex -> ex.Message |> Logger.error
        
        SingleInstance.stop()

        0
