namespace Example

open System
open System.Configuration
open System.ServiceProcess
open System.Configuration.Install
open System.ComponentModel

module constants = 
    let serviceName = "MyService"

type Svc() = 
    inherit ServiceBase(ServiceName = constants.serviceName)

    let endPoint = new EndPoint()

    override this.OnStart(args) =
        endPoint.StartEndPoint()
        //sprintf "%s service host started" constants.serviceName |> log 

    override this.OnStop() =
        endPoint.StopEndPoint()
        Async.CancelDefaultToken()
        //sprintf "%s service host stopped" constants.serviceName |> log 

[<RunInstaller(true)>]
type SvcHost() =
    inherit Installer()

    let defaultServiceName = constants.serviceName
    let serviceInstaller = 
        new ServiceInstaller    
            (   DisplayName = defaultServiceName,
                ServiceName = defaultServiceName,
                StartType = ServiceStartMode.Manual )        

    do
        new ServiceProcessInstaller(Account = ServiceAccount.LocalSystem) 
        |> base.Installers.Add |> ignore

        serviceInstaller 
        |> base.Installers.Add |> ignore 

    override this.Install(state) = 
        match this.Context.Parameters.ContainsKey("ServiceName") with
        | true ->
            serviceInstaller.DisplayName <- this.Context.Parameters.["ServiceName"]
            serviceInstaller.ServiceName <- this.Context.Parameters.["ServiceName"]
        | false -> ()
        base.Install(state)

    override this.Uninstall(state) =
        match this.Context.Parameters.ContainsKey("ServiceName") with
        | true ->
            serviceInstaller.DisplayName <- this.Context.Parameters.["ServiceName"]
            serviceInstaller.ServiceName <- this.Context.Parameters.["ServiceName"]
        | false -> ()
        base.Uninstall(state) 

module Main =         
    
    // ServiceBase.Run [| new Svc() :> ServiceBase |]

    let endPoint = new EndPoint()
    endPoint.StartEndPoint()
    Console.ReadLine() |> ignore