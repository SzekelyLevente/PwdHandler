namespace PwdHandler

open System
open System.IO
open DataAccess
open Logic
open Io.Io

module Main=
    [<EntryPoint>]
    let main args=
        if File.Exists "pwd.txt" then
            let (password,PwIv)=DataAccess.loadPassword()
            let registries=if File.Exists "registries.txt" then DataAccess.loadRegistries() else []
            let key=Crypto.fromBase64 (DataAccess.loadKey())
            let color = if File.Exists "color.txt" then DataAccess.loadColor() |> Coloring.fromString else ConsoleColor.White
            menuLoop LoginMenu registries (password, PwIv) key color
        else
            let key=Crypto.generateRandomKey()
            menuLoop SetBeginPasswordMenu [] ("","") key ConsoleColor.White
        0