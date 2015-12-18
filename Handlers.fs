namespace Example

open System.Net
open System.Net.Http
open System.ServiceModel.Channels
open System.Threading.Tasks
open Microsoft.Owin 

module Handlers =

    let getIpAddress (request: HttpRequestMessage) = 
        let owinCtx = 
            match request.Properties.ContainsKey("MS_OwinContext") with
            | true -> 
                Some(request.Properties.["MS_OwinContext"] :?> OwinContext)
            | false -> 
                None
        match owinCtx with 
        | None -> 
            match request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name) with
            | true -> 
                let remote = 
                    request.Properties.[RemoteEndpointMessageProperty.Name] :?> 
                        RemoteEndpointMessageProperty
                remote.Address
            | false -> ""
        | Some(owinCtx) -> owinCtx.Request.RemoteIpAddress

    let private getLocalIpAddresses () =
        let ipv6 =  
            Dns.GetHostEntry(Dns.GetHostName()).AddressList
            |> Array.filter(fun a -> 
                a.AddressFamily = System.Net.Sockets.AddressFamily.InterNetworkV6)
            |> List.ofArray
            |> List.head
        let ipv4 = 
            Dns.GetHostEntry(Dns.GetHostName()).AddressList
            |> Array.filter(fun a -> 
                a.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork)
            |> List.ofArray
            |> List.head
         
        (ipv6.ToString(), ipv4.ToString())

    let ipHandler = 
        { new DelegatingHandler() with 
            member x.SendAsync(request, cancellationToken) =
                let (localIpv6, localIpv4) = getLocalIpAddresses() 
                match getIpAddress request with 
                | ip when ip = localIpv6 -> 
                    printfn "%A" ip
                    base.SendAsync(request, cancellationToken)
                | ip when ip = localIpv4 -> 
                    base.SendAsync(request, cancellationToken)
                | ip ->
                    printfn "%A" ip 
                    let tcs = new TaskCompletionSource<HttpResponseMessage>()
                    tcs.SetResult(new HttpResponseMessage(HttpStatusCode.Forbidden))
                    tcs.Task }

    let private tokenHeaderName = "X-Token"

    let tokenInspector = 
        { new DelegatingHandler() with 
            member x.SendAsync(request, cancellationToken) =
                match request.RequestUri.ToString().Contains("api/login") with 
                | false -> 
                    let (userName,ipAddress) = 
                        match request.Headers.Contains(tokenHeaderName) with 
                        | true ->
                            request.Headers.GetValues(tokenHeaderName) 
                            |> Seq.head
                            |> CryptoHelpers.parseToken 
                        | false -> ("","") 
                    match getIpAddress request with
                    | x when x = ipAddress -> 
                        base.SendAsync(request, cancellationToken)
                    | _ -> 
                        request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid token")
                        |> Task.FromResult
                | true -> base.SendAsync(request,cancellationToken)  } 