namespace Example

open System
open System.Text
open System.Security.Cryptography
open System.Security.Cryptography.X509Certificates

module CryptoHelpers = 

    let subjectName = "CN=Example"

    let enumerate (collection:System.Collections.IEnumerable) =
        let en = collection.GetEnumerator()
        let rec loop (en:System.Collections.IEnumerator)(i:int) result = 
            match en.MoveNext() with
            | true -> 
                loop en (i+1) (en.Current :?> 'a :: result)
            | false -> result
        loop en 0 List.empty

    let getX509Certificate =
        let store = new X509Store(StoreName.My, StoreLocation.LocalMachine)
        store.Open(OpenFlags.ReadOnly)
        let cert = 
            let certs = 
                store.Certificates
                |> enumerate 
                |> List.filter(fun (f:X509Certificate2) -> 
                    f.SubjectName.Name.Equals(subjectName, StringComparison.OrdinalIgnoreCase))
            match certs |> List.isEmpty with
            | true -> None
            | false -> Some(certs.Head)
        store.Close()
        cert

    let encrypt (cert:X509Certificate2) (plainToken:string) = 
        let crypto = cert.PublicKey.Key :?> RSACryptoServiceProvider
        crypto.Encrypt(Encoding.UTF8.GetBytes(plainToken),true)
        |> Convert.ToBase64String

    let decrypt (cert:X509Certificate2) (encryptedToken:string) = 
        let crypto = cert.PrivateKey :?> RSACryptoServiceProvider
        let v = 
            encryptedToken
            |> Convert.FromBase64String
        crypto.Decrypt(v, true)
        |> Encoding.UTF8.GetString

    let createToken (userName, ipAddress) = 
        let txt = sprintf "%s;%s" userName ipAddress
        match getX509Certificate with
        | None -> String.Empty
        | Some(cert) -> encrypt cert txt

    let parseToken (token:string) =
        try 
            match getX509Certificate with
            | None -> (String.Empty,String.Empty)
            | Some(cert) -> 
                match decrypt cert token with 
                | null -> (String.Empty, String.Empty)
                | decrypted -> 
                    let parts = decrypted.Split([|';'|])
                    parts.[0], parts.[1]
        with ex -> 
            printfn "%A" ex
            new Exception("Bad Token") |> raise


