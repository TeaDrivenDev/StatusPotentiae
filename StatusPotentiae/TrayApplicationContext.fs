﻿namespace TeaDriven.StatusPotentiae

module ApplicationContext =
    open System.ComponentModel
    open System.Reflection
    open System.Windows.Forms

    type TrayApplicationContext() as this =
        inherit ApplicationContext()

        let components = new Container()

        let onContextMenuStripOpening (sender : obj) (e : CancelEventArgs) =
            let contextMenuStrip = sender :?> ContextMenuStrip

            let activePlan = PowerManagement.getActivePlan ()

            for item in contextMenuStrip.Items do
                match item with
                | :? ToolStripMenuItem as item ->
                    item.Checked <-
                        match item.Tag with
                        | null -> false
                        | tag -> tag :?> PowerManagement.PowerPlan = activePlan
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

        let (|AtLeast|_|) atLeast inputValue =
            if inputValue >= atLeast then Some () else None

        let updateBatteryDisplay planName isCharging percentValue =
            let icon =
                match percentValue, isCharging with
                | AtLeast 86, true -> Resources.Icons.batt_ch_4
                | AtLeast 86, false -> Resources.Icons.batt_4
                | AtLeast 62, true -> Resources.Icons.batt_ch_3
                | AtLeast 62, false -> Resources.Icons.batt_3
                | AtLeast 38, true -> Resources.Icons.batt_ch_2
                | AtLeast 38, false -> Resources.Icons.batt_2
                | AtLeast 14, true -> Resources.Icons.batt_ch_1
                | AtLeast 14, false -> Resources.Icons.batt_1
                | _, true -> Resources.Icons.batt_ch_0
                | _, false -> Resources.Icons.batt_0
                |> Resources.getIcon

            notifyIcon.Icon <- icon
            notifyIcon.Text <- sprintf "%s (%i%%)" planName percentValue

        let updateBatteryState () =
            updateBatteryDisplay
                (PowerManagement.getActivePlan ()).Name
                (PowerManagement.isCharging ())
                (PowerManagement.getChargeValue ())

        let refreshTimer =
            let timer = new Timer(components, Interval = 5 * 1000)
            timer.Tick.AddHandler(fun _ _ -> updateBatteryState ())
            timer.Enabled <- true

            timer

        let plans = PowerManagement.getPlans ()
    
        let addMenuItems() =
            let createDefaultPlanMenu
                (displayName : string)
                getDefault
                setDefault
                (plans : PowerManagement.PowerPlan list) =
                let menu = new ToolStripMenuItem(displayName)
                
                plans
                |> List.iter (fun plan ->
                    let item = new ToolStripMenuItem(plan.Name)
                    item.Tag <- plan
                    item.Click.AddHandler (fun _ _ ->
                        setDefault plan.Guid)
                    item.Checked <-
                        getDefault ()
                        |> Option.map ((=) plan.Guid)
                        |> Option.defaultValue false

                    item |> menu.DropDownItems.Add |> ignore)
                
                menu
    
            let currentPlan = PowerManagement.getActivePlan ()

            let menuItems = notifyIcon.ContextMenuStrip.Items

            plans
            |> List.iter (fun plan ->
                let item = new ToolStripMenuItem(plan.Name)
                item.Tag <- plan
                item.Click.AddHandler(fun _ _ ->
                    PowerManagement.setActive updateBatteryState plan)
                item.Checked <- (plan = currentPlan)

                item |> menuItems.Add |> ignore)

            new ToolStripSeparator() |> menuItems.Add |> ignore

            plans
            |> createDefaultPlanMenu
                "Connected Power Plan"
                Settings.getConnectedPowerPlan
                Settings.setConnectedPowerPlan
            |> menuItems.Add
            |> ignore

            plans
            |> createDefaultPlanMenu
                "Disconnected Power Plan"
                Settings.getDisconnectedPowerPlan
                Settings.setDisconnectedPowerPlan
            |> menuItems.Add
            |> ignore

            new ToolStripSeparator() |> notifyIcon.ContextMenuStrip.Items.Add |> ignore

            let item = new ToolStripMenuItem("Open Power Options")
            item.Click.AddHandler(fun _ _ -> PowerManagement.openPowerOptions ())
            item |> menuItems.Add |> ignore

            let item = new ToolStripMenuItem("Install Autostart Shortcut")
            item.Click.AddHandler(fun _ _ -> Autostart.createAutostartShortcut "Status Potentiae")
            item |> menuItems.Add |> ignore

            let item = new ToolStripMenuItem("Exit")
            item.Click.AddHandler(fun _ _ -> this.ExitThread())
            item |> menuItems.Add |> ignore

        do
            addMenuItems ()
            updateBatteryState ()

            PowerManagement.registerPowerModeChangedHandler updateBatteryState

        override __.Dispose disposing =
            if disposing && not <| isNull components
            then components.Dispose()
