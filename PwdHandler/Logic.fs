namespace Logic

open System.Security.Cryptography
open System.IO
open System

type Registry= {
    Name : string ;
    Password : byte array ;
    Iv: byte array ;
}

type MenuState =
    | MainMenu
    | LoginMenu
    | SettingsMenu
    | RegistriesMenu
    | AddRegistryMenu
    | RemoveRegistry
    | UpdateRegistry
    | PasswordGeneratorMenu
    | SetPasswordMenu
    | SetBeginPasswordMenu
    | SetColorMenu
    | Exit

module Crypto=

    let encrypt (plainText: string) (key: byte[]) (iv: byte[]) =
        use aes = Aes.Create()
        aes.Key <- key
        aes.IV <- iv

        use encryptor = aes.CreateEncryptor(aes.Key, aes.IV)
        use ms = new MemoryStream()
        use cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write)
        use sw = new StreamWriter(cs)
        sw.Write(plainText)
        sw.Close()
        ms.ToArray()

    let decrypt (cipherBytes: byte[]) (key: byte[]) (iv: byte[]) =
        use aes = Aes.Create()
        aes.Key <- key
        aes.IV <- iv

        use decryptor = aes.CreateDecryptor(aes.Key, aes.IV)
        use ms = new MemoryStream(cipherBytes)
        use cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)
        use sr = new StreamReader(cs)
        sr.ReadToEnd()

    let generateRandomIV () =
        let iv = Array.zeroCreate<byte> 16
        use rng = RandomNumberGenerator.Create()
        rng.GetBytes(iv)
        iv

    let toBase64 (bytes: byte[]) : string =
        System.Convert.ToBase64String(bytes)

    let fromBase64 (base64: string) : byte[] =
        System.Convert.FromBase64String(base64)

    let generateRandomKey () =
        use aes = Aes.Create()
        aes.KeySize <- 256
        aes.GenerateKey()
        aes.Key

module ListOperations=

    let addItem items item=
        let revList=List.rev items
        (item::revList) |> List.rev

    let removeItem items item=
        items |> List.filter (fun x -> x <> item)

    let updateItem items item newItem=
        items |> List.map (fun x -> if x = item then newItem else x)

module PasswordGenerate=

    let generate length bigaz smallaz numbers specs (random:Random)=
        let bigChars=['A'..'Z']
        let smallChars=['a'..'z']
        let numbersChars=['0'..'9']
        let symbols = ['!'; '@'; '#'; '$'; '%'; '^'; '&'; '*']
        let charset =
            [ 
              if bigaz then bigChars
              if smallaz then smallChars 
              if numbers then numbersChars
              if specs then symbols]
            |> List.concat
        List.init length (fun _ -> charset[random.Next(charset.Length)])
        |> List.toArray
        |> String

module Coloring=
    
    let toString color=
        match color with
        | ConsoleColor.Red -> "Red"
        | ConsoleColor.Blue -> "Blue"
        | ConsoleColor.Green -> "Green"
        | ConsoleColor.Yellow -> "Yellow"
        | ConsoleColor.White -> "White"
        | ConsoleColor.Magenta -> "Magenta"

    let fromString str=
        match str with
        | "Red" -> ConsoleColor.Red
        | "Blue" -> ConsoleColor.Blue
        | "Green" -> ConsoleColor.Green
        | "Yellow" -> ConsoleColor.Yellow
        | "White" -> ConsoleColor.White
        | "Magenta" -> ConsoleColor.Magenta