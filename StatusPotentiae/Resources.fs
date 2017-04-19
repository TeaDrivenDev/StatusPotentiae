namespace TeaDriven.StatusPotentiae

[<RequireQualifiedAccess>]
module Resources =
    open System.Globalization
    open System.Resources

    let private resources = ResourceManager("Resources", System.Reflection.Assembly.GetExecutingAssembly())

    [<RequireQualifiedAccess>]
    module Icons =
        let batt_0 = "batt_0"
        let batt_1 = "batt_1"
        let batt_2 = "batt_2"
        let batt_3 = "batt_3"
        let batt_4 = "batt_4"
        let batt_ch_0 = "batt_ch_0"
        let batt_ch_1 = "batt_ch_1"
        let batt_ch_2 = "batt_ch_2"
        let batt_ch_3 = "batt_ch_3"
        let batt_ch_4 = "batt_ch_4"

    let getIcon key =
        resources.GetObject(key, CultureInfo.InvariantCulture) :?> System.Drawing.Icon