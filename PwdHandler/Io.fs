namespace Io

open Logic
open DataAccess
open System

module Io=
    let rec readPasswordRec (acc: char list) =
        let key = Console.ReadKey(true)
        match key.Key with
        | ConsoleKey.Enter ->
            Console.WriteLine()
            acc |> List.rev |> Array.ofList |> String

        | ConsoleKey.Backspace ->
            match acc with
            | [] -> readPasswordRec acc
            | _ :: tail ->
                Console.Write("\b \b")
                readPasswordRec tail

        | _ when not (Char.IsControl key.KeyChar) ->
            Console.Write("*")
            readPasswordRec (key.KeyChar :: acc)

        | _ -> readPasswordRec acc

    let readPassword (prompt:string) =
        Console.Write(prompt)
        readPasswordRec []

    let readLineWithDefault (defaultValue: string) =
        let input = Console.ReadLine()
        if String.IsNullOrWhiteSpace(input) then defaultValue
        else input

    let readLineWithDefaultPassword (defaultValue: string) (prompt:string)=
        let input = readPassword prompt
        if String.IsNullOrWhiteSpace(input) then defaultValue
        else input

    let rec menuLoop state registries password key color=
        Console.Clear()
        Console.ForegroundColor <- color
        match state with
        | MainMenu ->
            printfn "==== Main ===="
            printfn "1. Registries"
            printfn "2. Add registry"
            printfn "3. Remove registry"
            printfn "4. Update registry"
            printfn "5. Password generator"
            printfn "6. Settings"
            printfn "0. Exit"
            printf "-> "
            match Console.ReadLine() with
            | "0" -> menuLoop Exit registries password key color
            | "1" -> menuLoop RegistriesMenu registries password key color
            | "2" -> menuLoop AddRegistryMenu registries password key color
            | "3" -> menuLoop RemoveRegistry registries password key color
            | "4" -> menuLoop UpdateRegistry registries password key color
            | "5" -> menuLoop PasswordGeneratorMenu registries password key color
            | "6" -> menuLoop SettingsMenu registries password key color
            | _   -> 
                printfn "Wrong input! Please press a button..."
                Console.ReadKey() |> ignore
                menuLoop MainMenu registries password key color

        | LoginMenu ->
            printfn "==== Login ===="
            let (pwd,pwdIv) = password
            let input=readPassword "Password: "
            if pwd = (Crypto.encrypt input key (pwdIv |> Crypto.fromBase64) |> Crypto.toBase64) then
                menuLoop MainMenu registries password key color
            else
                printfn "Wrong password! Please try again"
                Console.ReadKey() |> ignore
                menuLoop LoginMenu registries password key color

        | SetPasswordMenu ->
            printfn "==== Set the password ===="
            printfn "0. Back"
            let newPassword=readPassword "New password: "
            match newPassword with
            | "0" -> menuLoop SettingsMenu registries password key color
            | _ ->
                let newPasswordagain=readPassword "New password again: "
                if newPassword = newPasswordagain then
                    let iv=Crypto.generateRandomIV()
                    let pwd=((Crypto.encrypt newPassword key iv) |> Crypto.toBase64,Crypto.toBase64 iv)
                    DataAccess.savePassword pwd
                    menuLoop LoginMenu registries pwd key color
                else
                    printfn "The password confirm doesn't match! Press a button..."
                    Console.ReadKey() |> ignore
                    menuLoop SetPasswordMenu registries password key color

        | SetBeginPasswordMenu ->
            printfn "==== Set the password ===="
            let newPassword=readPassword "New password: "
            let newPasswordagain=readPassword "New password again: "
            if newPassword = newPasswordagain then
                let iv=Crypto.generateRandomIV()
                let pwd=((Crypto.encrypt newPassword key iv) |> Crypto.toBase64,Crypto.toBase64 iv)
                DataAccess.savePassword pwd
                DataAccess.saveKey (Crypto.toBase64 key)
                menuLoop LoginMenu registries pwd key color
            else
                printfn "The password confirm doesn't match! Press a button..."
                Console.ReadKey() |> ignore
                menuLoop SetPasswordMenu registries password key color

        | SettingsMenu ->
            printfn "==== Settings ===="
            printfn "1. Change password"
            printfn "2. Set color"
            printfn "0. Back"
            printf "-> "
            match Console.ReadLine() with
            | "0" -> menuLoop MainMenu registries password key color
            | "1" -> menuLoop SetPasswordMenu registries password key color
            | "2" -> menuLoop SetColorMenu registries password key color
            | _   -> 
                printfn "Wrong input! Please press a button..."
                Console.ReadKey() |> ignore
                menuLoop SettingsMenu registries password key color

        | RegistriesMenu ->
            printfn "==== Registries ===="
            if List.length registries > 0 then
                registries |> List.iteri (fun i item -> printfn "%i. %s" (i+1) item.Name)
            else
                printfn "The list is empty"
            printfn ""
            printfn "Pick a number to show password"
            printfn "0. Back"
            printf "-> "
            match Int32.TryParse(Console.ReadLine()) with
            | (true, 0) -> menuLoop MainMenu registries password key color
            | (true, index) when index > 0 && index <= List.length registries ->
                let registry=registries.[index-1]
                let pwd=Crypto.decrypt registry.Password key registry.Iv
                printfn "Password: %s" pwd
                printfn ""
                printfn "Press a button..."
                Console.ReadKey() |> ignore
                menuLoop RegistriesMenu registries password key color
            | _   ->
                printfn "Wrong input! Please press a button..."
                Console.ReadKey() |> ignore
                menuLoop RegistriesMenu registries password key color

        | AddRegistryMenu ->
            printfn "==== Add registry ===="
            printfn "0. Back"
            printf "Name: "
            let name=Console.ReadLine()
            match name with
            | "0" -> menuLoop MainMenu registries password key color
            | _ ->
                let pwd=readPassword "Password: "
                let iv=Crypto.generateRandomIV()
                let encryptedPwd=Crypto.encrypt pwd key iv
                let newRegistries=ListOperations.addItem registries ({Name=name; Password=encryptedPwd; Iv=iv})
                DataAccess.saveRegistries newRegistries
                printfn "New registry added! Press a button..."
                Console.ReadKey() |> ignore
                menuLoop MainMenu newRegistries password key color

        | RemoveRegistry ->
            printfn "==== Remove registry ===="
            if List.length registries > 0 then
                registries |> List.iteri (fun i item -> printfn "%i. %s" (i+1) item.Name)
            else
                printfn "The list is empty"
            printfn ""
            printfn "Pick a number to remove a registry"
            printfn "0. Back"
            printf "-> "
            match Int32.TryParse(Console.ReadLine()) with
            | (true, 0) -> menuLoop MainMenu registries password key color
            | (true, index) when index > 0 && index <= List.length registries ->
                printfn "Are you sure to remove this registry? (y/_)"
                match Console.ReadLine() with
                | "y" ->
                    let registry=registries.[index-1]
                    let newRegistries=ListOperations.removeItem registries registry
                    DataAccess.saveRegistries newRegistries
                    printfn "Registry removed! Press a button..."
                    Console.ReadKey() |> ignore
                    menuLoop MainMenu newRegistries password key color
                | _   -> menuLoop RemoveRegistry registries password key color
            | _   ->
                printfn "Wrong input! Please press a button..."
                Console.ReadKey() |> ignore
                menuLoop RemoveRegistry registries password key color

        | UpdateRegistry ->
            printfn "==== Update registry ===="
            if List.length registries > 0 then
                registries |> List.iteri (fun i item -> printfn "%i. %s" (i+1) item.Name)
            else
                printfn "The list is empty"
            printfn ""
            printfn "Pick a number to update a registry"
            printfn "0. Back"
            printf "-> "
            match Int32.TryParse(Console.ReadLine()) with
            | (true, 0) -> menuLoop MainMenu registries password key color
            | (true, index) when index > 0 && index <= List.length registries ->
                let actRegistry=registries.[index-1]
                let decryptedPwd=Crypto.decrypt actRegistry.Password key actRegistry.Iv
                printfn "Press enter for the old name"
                printf "New name: "
                let newName=readLineWithDefault actRegistry.Name
                printfn "Press enter for the old password"
                let newPassword=readLineWithDefaultPassword decryptedPwd "New password: "
                let encryptedPwd=Crypto.encrypt newPassword key actRegistry.Iv
                let newRegistry={Name=newName;Password=encryptedPwd;Iv=actRegistry.Iv}
                let newRegistries=ListOperations.updateItem registries actRegistry newRegistry
                DataAccess.saveRegistries newRegistries
                printfn "Registry updated! Press a button..."
                Console.ReadKey() |> ignore
                menuLoop MainMenu newRegistries password key color
            | _   ->
                printfn "Wrong input! Please press a button..."
                Console.ReadKey() |> ignore
                menuLoop RemoveRegistry registries password key color

        | PasswordGeneratorMenu ->
            printfn "==== Password generator ===="
            printfn "Command syntax: length(1-50) A-Z(t,f) a-z(t,f) numbers(t,f) special characters(t,f)"
            printfn "0. Back"
            printf "-> "
            let command=Console.ReadLine()
            match command with
            | "0" -> menuLoop MainMenu registries password key color
            | _ ->
                try
                    let parts=command.Split(' ')
                    let length=int parts[0]
                    let bigChars=if parts[1]="t" then true else false
                    let smallChars=if parts[2]="t" then true else false
                    let numbersChars=if parts[3]="t" then true else false
                    let specs=if parts[4]="t" then true else false
                    let random=Random()
                    let generatedPassword=PasswordGenerate.generate length bigChars smallChars numbersChars specs random
                    printfn "Generated password:"
                    printfn "%s" generatedPassword
                    printfn ""
                    printfn "Please press a button..."
                    Console.ReadKey() |> ignore
                    menuLoop PasswordGeneratorMenu registries password key color
                with
                | :? System.Exception ->
                    printfn "Wrong input! Please press a button..."
                    Console.ReadKey() |> ignore
                    menuLoop PasswordGeneratorMenu registries password key color

        | SetColorMenu ->
            printfn "==== Set color ===="
            printfn "1. Red"
            printfn "2. Blue"
            printfn "3. Green"
            printfn "4. Yellow"
            printfn "5. White"
            printfn "6. Magenta"
            printfn "0. Back"
            printf "->"
            let input=Console.ReadLine()
            match input with
            | "0" -> menuLoop SettingsMenu registries password key color
            | "1" ->
                Coloring.toString ConsoleColor.Red |> DataAccess.saveColor
                menuLoop SetColorMenu registries password key ConsoleColor.Red
            | "2" ->
                Coloring.toString ConsoleColor.Blue |> DataAccess.saveColor
                menuLoop SetColorMenu registries password key ConsoleColor.Blue
            | "3" ->
                Coloring.toString ConsoleColor.Green |> DataAccess.saveColor
                menuLoop SetColorMenu registries password key ConsoleColor.Green
            | "4" ->
                Coloring.toString ConsoleColor.Yellow |> DataAccess.saveColor
                menuLoop SetColorMenu registries password key ConsoleColor.Yellow
            | "5" ->
                Coloring.toString ConsoleColor.White |> DataAccess.saveColor
                menuLoop SetColorMenu registries password key ConsoleColor.White
            | "6" ->
                Coloring.toString ConsoleColor.Magenta |> DataAccess.saveColor
                menuLoop SetColorMenu registries password key ConsoleColor.Magenta
            | _   -> 
                printfn "Wrong input! Please press a button..."
                Console.ReadKey() |> ignore
                menuLoop SetColorMenu registries password key color

        | Exit -> printfn "Exiting..."