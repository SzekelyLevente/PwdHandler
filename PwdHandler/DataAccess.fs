namespace DataAccess

open Logic
open System.IO

module FileOperations=

    let writeFileText (filePath:string) (content:string)=
        File.WriteAllText(filePath,content)

    let writeFileLines (filePath:string) (content:string list)=
        File.WriteAllLines(filePath,content)

    let readFileLines (filePath: string) : string list =
        if File.Exists filePath then
            seq {
                use reader = new StreamReader(filePath)
                while not reader.EndOfStream do
                    yield reader.ReadLine()
            }
            |> Seq.toList
        else
            List.empty

    let readFileText (filePath:string)=
        File.ReadAllText filePath

    let fileExists fileName=
        File.Exists fileName

module DataAccess=

    let loadRegistries()=
        FileOperations.readFileLines "registries.txt"
        |> List.map (fun item ->
            let fields=item.Split(";")
            let password=Crypto.fromBase64(fields.[1])
            let iv=Crypto.fromBase64(fields.[2])
            {Name=fields.[0] ; Password=password ; Iv=iv}
        )

    let saveRegistries registries=
        registries
        |> List.map (fun item ->
            let password=Crypto.toBase64(item.Password)
            let iv=Crypto.toBase64(item.Iv)
            item.Name + ";" + password + ";" + iv
        )
        |> FileOperations.writeFileLines "registries.txt"

    let loadPassword()=
        let line=FileOperations.readFileText "pwd.txt"
        let datas=line.Split(";")
        (datas.[0],datas.[1])

    let savePassword (password,iv)=
        let content=password + ";" + iv
        FileOperations.writeFileText "pwd.txt" content

    let saveKey key=
        FileOperations.writeFileText "key.txt" key

    let loadKey()=
        FileOperations.readFileText "key.txt"

    let loadColor()=
        FileOperations.readFileText "color.txt"

    let saveColor color=
        FileOperations.writeFileText "color.txt" color