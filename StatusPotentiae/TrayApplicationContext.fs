namespace TeaDriven.StatusPotentiae

module ApplicationContext =
    open System.ComponentModel
    open System.Reflection
    open System.Windows.Forms

    type TrayApplicationContext() as this =
        inherit ApplicationContext()

        let components = new System.ComponentModel.Container()

        let onContextMenuStripOpening (sender : obj) (e : CancelEventArgs) =
            let icon = sender :?> NotifyIcon

            let activePlan = PowerManagement.getActivePlan ()

            for item in icon.ContextMenuStrip.Items do
                match item with
                | :? ToolStripMenuItem as item ->
                    item.Checked <- item.Tag :?> PowerManagement.PowerPlan = activePlan
                | _ -> ()

        let onNotifyIconMouseUp (sender : obj) (e : MouseEventArgs) =
            let notifyIcon = sender :?> NotifyIcon

            if e.Button = MouseButtons.Left
            then
                let mi = typeof<NotifyIcon>.GetMethod("ShowContextMenu", BindingFlags.Instance ||| BindingFlags.NonPublic)
                mi.Invoke(notifyIcon, null) |> ignore


        let notifyIcon =
            let icon =
                new NotifyIcon(components,
                               ContextMenuStrip = new ContextMenuStrip(),
                               Icon = null,
                               Text = "Status Potentiae",
                               Visible = true)
                
            icon.ContextMenuStrip.Opening.AddHandler(CancelEventHandler onContextMenuStripOpening)
            icon.MouseUp.AddHandler(MouseEventHandler onNotifyIconMouseUp)

            icon

        let updateBatteryDisplay planName isCharging percentValue =
            ()

        let updateBatteryState () =
            updateBatteryDisplay
                (PowerManagement.getActivePlan ())
                (PowerManagement.isCharging ())
                (PowerManagement.getChargeValue ())

        let refreshTimer =
            let timer = new Timer(components, Interval = 5 * 1000)
            timer.Tick.AddHandler(fun _ _ -> updateBatteryState ())
            timer.Enabled <- true

            timer

        let plans = PowerManagement.getPlans ()
    
        let addMenuItems() =
            let currentPlan = PowerManagement.getActivePlan ()

            plans
            |> List.iter (fun plan ->
                let item = new ToolStripMenuItem(plan.Name)
                item.Tag <- plan
                item.Click.AddHandler(fun _ _ ->
                    PowerManagement.setActive updateBatteryState plan)

                item.Checked <- (plan = currentPlan)

                notifyIcon.ContextMenuStrip.Items.Add item |> ignore)

            let item = new ToolStripMenuItem("Open Power Options")
            item.Click.AddHandler(fun _ _ -> PowerManagement.openPowerOptions ())
            notifyIcon.ContextMenuStrip.Items.Add item |> ignore

            let item = new ToolStripMenuItem("Exit")
            item.Click.AddHandler(fun _ _ -> this.ExitThread())
            notifyIcon.ContextMenuStrip.Items.Add item |> ignore

        do
            addMenuItems ()
            updateBatteryState ()

        override __.Dispose disposing =
            if disposing && not <| isNull components
            then components.Dispose()
