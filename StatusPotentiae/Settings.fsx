
#I "..\packages"

#r @"FAKE.Lib\lib\net451\FakeLib.dll"

open System
open System.IO

open Microsoft.Win32

open Fake

let software = getRegistryKey HKEYCurrentUser "Software" true

let teaDriven = software.CreateSubKey("TeaDriven").CreateSubKey("StatusPotentiae")

let guid = Guid "78b3f1b2-3bbf-439d-a521-2f730a8a0367"

let value = teaDriven.SetValue("Guid", guid, RegistryValueKind.Binary)

RegistryHelper.setRegistryValue HKEYCurrentUser @"Software\TeaDriven\StatusPotentiae" "Guid" (guid.ToString "B")


//let tryGetRegistryValue baseKey subKey name =
//    if valueExistsForKey baseKey subKey name
//    then getRegistryValue baseKey subKey name |> Some
//    else None

//let mode = tryGetRegistryValue HKEYCurrentUser @"Software\TeaDriven\StatusPotentiae" "Guid"

let asFst second first = first, second

let private tryGetRegistryValue baseKey (subKey : string) name =
    let rec getKey current remaining =
        match remaining with
        | [] -> None
        | head :: tail ->
            let newCurrent = 
                [ current; head ]
                |> String.concat "\\"
            
            newCurrent
            |> asFst false
            ||> getRegistryKey baseKey
            |> Option.ofObj
            |> Option.bind (fun _ -> getKey newCurrent tail)

    subKey.Split [| '\\' |]
    |> Array.toList
    |> getKey ""
    |> Option.bind (fun _ ->
        if valueExistsForKey baseKey subKey name
        then getRegistryValue baseKey subKey name |> Some
        else None)

tryGetRegistryValue HKEYCurrentUser @"Software\TeaDriven\StatusPotentiae" "Guid"

createRegistrySubKey HKEYCurrentUser @"Software\TeaDriven\StatusPotentiae"


let v = System.Version(4, 3, 2, 1)
v.MinorRevision