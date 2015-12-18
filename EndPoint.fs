namespace Example

open System
open Owin
open System.Web.Http
open System.Configuration
open Microsoft.Owin.Hosting 

type Startup() = 
    member this.Configuration(app: IAppBuilder) = 
        try
            let config = new HttpConfiguration() 
            config.MapHttpAttributeRoutes()
            config.MessageHandlers.Add(Handlers.ipHandler)
            config.MessageHandlers.Add(Handlers.tokenInspector)
            //app.UseCors(CorsOptions.AllowAll) |> ignore
            app.UseWebApi(config) |> ignore 
        with ex -> 
            printfn "%A" ex
            //sprintf "%A" ex |> log  

type EndPointState = 
    | Running of IDisposable
    | NotRunning

type AgentSignal = 
    | Start
    | Stop


type EndPoint() = 
    let agent = MailboxProcessor.Start(fun inbox ->
        let rec loop state = async {
            let! msg = inbox.Receive()
            match msg, state with 
            | Start, NotRunning -> 
                let baseAddress = ConfigurationManager.AppSettings.["baseAddress"]
                let server = WebApp.Start<Startup>(baseAddress)
                    
                //sprintf "Endpoint listening at %s" baseAddress |> log
                return! loop (Running(server)) 
            | Start, Running(_)
            | Stop, NotRunning ->
                return! loop state 
            | Stop, Running(server) -> 
                server.Dispose()
                return! loop NotRunning                 
        }
        loop NotRunning)

    member this.StartEndPoint() = 
        agent.Post(Start) 

    member this.StopEndPoint() = 
        agent.Post(Stop)

