[<AutoOpen>]
module Farmer.Builders.ApplicationGateway

open System
open Farmer
open Farmer.Arm.AzureFirewall
open Farmer.Arm.Network
open Farmer.PublicIpAddress
open Farmer.Arm.ApplicationGateway
open Farmer.ApplicationGateway

type GatewayIpConfig = 
    {
        Name: ResourceName
        Subnet: LinkedResource option 
    }
    static member BuildResource gatewayIp =
        {|
            Name = gatewayIp.Name
            Subnet =
                gatewayIp.Subnet
                |> Option.map (function | Managed resId -> resId | Unmanaged resId -> resId)
        |}

type GatewayIpBuilder() = 
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Subnet = None
        }
    [<CustomOperation "name">]
    member _.Name(state:GatewayIpConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "link_to_subnet">]
    member _.LinktoSubnet(state:GatewayIpConfig, subnet:string) =
        { state with Subnet = Some (Unmanaged (virtualNetworks.resourceId (ResourceName subnet))) }

let gatewayIp = GatewayIpBuilder()

// Subtle differences between load balancers
type FrontendIpConfig =
    {
        Name: ResourceName
        PrivateIpAllocationMethod: PrivateIpAddress.AllocationMethod
        PublicIp: LinkedResource option
    }
    static member BuildResource frontend =
        {|
            Name = frontend.Name
            PrivateIpAllocationMethod = frontend.PrivateIpAllocationMethod
            PublicIp =
                frontend.PublicIp
                |> Option.map (function | Managed resId -> resId | Unmanaged resId -> resId)
        |}
    static member BuildIp (frontend:FrontendIpConfig) (location:Location) : PublicIpAddress option =
        match frontend.PublicIp with
        | Some (Managed resId) ->
            {
                Name = resId.Name
                AllocationMethod = AllocationMethod.Static
                Location = location
                Sku = PublicIpAddress.Sku.Standard
                DomainNameLabel = None
                Tags = Map.empty
            } |> Some
        | _ -> None

type FrontendIpBuilder () =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            PrivateIpAllocationMethod = PrivateIpAddress.DynamicPrivateIp
            PublicIp = None
        }
    /// Sets the name of the frontend IP configuration.
    [<CustomOperation "name">]
    member _.Name(state:FrontendIpConfig, name) = { state with Name = ResourceName name }
    /// Sets the frontend's private IP allocation method.
    [<CustomOperation "private_ip_allocation_method">]
    member _.PrivateIpAllocationMethod(state:FrontendIpConfig, allocationMethod) =
        { state with PrivateIpAllocationMethod = allocationMethod  }
    /// Sets the name of the frontend public IP.
    [<CustomOperation "public_ip">]
    member _.PublicIp(state:FrontendIpConfig, publicIp) = { state with PublicIp = Some (Managed (Farmer.Arm.Network.publicIPAddresses.resourceId (ResourceName publicIp))) }
    /// Links the frontend to an existing public IP.
    [<CustomOperation "link_to_public_ip">]
    member _.LinkToPublicIp(state:FrontendIpConfig, publicIp:string) = { state with PublicIp = Some (Unmanaged (virtualNetworks.resourceId (ResourceName publicIp))) }

let frontendIp = FrontendIpBuilder()

type FrontendPortConfig = 
    {
        Name: ResourceName
        Port: uint16
    }
    static member BuildResource frontendPort =
        {|
            Name = frontendPort.Name
            Port = frontendPort.Port
        |}

type FrontendPortBuilder () = 
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            Port = uint16 80
        }
    [<CustomOperation "name">]
    member _.Name(state:FrontendPortConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "port">]
    member _.Port(state:FrontendPortConfig, port) =
        { state with Port = port }

let frontendPort = FrontendPortBuilder()

type BackendAddressConfig = 
    {
        Fqdn : string
        IpAddress : System.Net.IPAddress
    }
    static member BuildResource backendAddress = 
        {|
            Fqdn = backendAddress.Fqdn
            IpAddress = backendAddress.IpAddress
        |}

type BackendAddressBuilder () =
    member _.Yield _ =
        {
            Fqdn = ""
            IpAddress = System.Net.IPAddress.None
        }
    [<CustomOperation "fqdn">]
    member _.Fqdn (state:BackendAddressConfig, fqdn) =
        { state with Fqdn = fqdn }
    [<CustomOperation "ip_address">]
    member _.IpAddress (state:BackendAddressConfig, ipAddress:string) =
        { state with IpAddress = System.Net.IPAddress.Parse ipAddress }

let backendAddress = BackendAddressBuilder()

type BackendAddressPoolConfig =
    {
        Name: ResourceName
        ApplicationGateway: ResourceName
        BackendAddresses: BackendAddressConfig list
    }
    interface IBuilder with
        member this.ResourceId = ApplicationGatewayBackendAddressPools.resourceId (this.ApplicationGateway, this.Name)
        member this.BuildResources _ =
            if String.IsNullOrWhiteSpace (this.ApplicationGateway.Value) then
                raiseFarmer "Application Gateway must be specified for backend address pool."
            else
                [
                    { Name = this.Name
                      ApplicationGateway = this.ApplicationGateway
                      ApplicationGatewayBackendAddresses = 
                        this.BackendAddresses |> List.map (fun backendAddress ->
                            {|
                                Fqdn = backendAddress.Fqdn
                                IpAddress = backendAddress.IpAddress
                            |}
                      )
                    }
                ]

type BackendAddressPoolBuilder () =
    member _.Yield _ = 
        {
            Name = ResourceName.Empty
            ApplicationGateway = ResourceName.Empty
            BackendAddresses = []
        }
    [<CustomOperation "name">]
    member _.Name (state:BackendAddressPoolConfig, name) = 
        { state with Name = ResourceName name }
    [<CustomOperation "application_gateway">]
    member _.ApplicationGateway (state:BackendAddressPoolConfig, applicationGateway) = 
        { state with ApplicationGateway = ResourceName applicationGateway }
    [<CustomOperation "add_backend_addresses">]
    member _.BackendAddresses (state:BackendAddressPoolConfig, backendAddresses) = 
        { state with BackendAddresses = state.BackendAddresses @ backendAddresses }

let appGatewayBackendAddressPool = BackendAddressPoolBuilder()

type ConnectionDrainingConfig = 
    {
        DrainTimeoutInSeconds: int<Seconds>
        Enabled: bool 
    }
    static member BuildResource connDraining = 
        {|
            DrainTimeoutInSeconds = connDraining.DrainTimeoutInSeconds
            Enabled = connDraining.Enabled
        |}

type ConnectionDrainingBuilder () = 
    member _.Yield _ =
        {
            DrainTimeoutInSeconds = 0<Seconds>
            Enabled = false
        }
    [<CustomOperation "drain_timeout">]
    member _.DrainTimeoutInSeconds (state:ConnectionDrainingConfig, timeout) =
        { state with DrainTimeoutInSeconds = timeout }
    [<CustomOperation "enabled">]
    member _.Enabled (state:ConnectionDrainingConfig, enabled) = 
        { state with Enabled = enabled }

let connectionDraining = ConnectionDrainingBuilder()

type BackendHttpSettingsConfig = 
    {
        Name: ResourceName
        AffinityCookieName: string option
        AuthenticationCertificates: ResourceName list
        ConnectionDraining: ConnectionDrainingConfig option
        CookieBasedAffinity: FeatureFlag
        HostName: string option
        Path: string option
        Port: uint16
        Protocol: Protocol
        PickHostNameFromBackendAddress: bool
        RequestTimeoutInSeconds: int<Seconds>
        Probe: ResourceName option
        ProbeEnabled : bool
        TrustedRootCertificates : ResourceName list
    }
    static member BuildResource backendHttpSettings =
        {|
            Name = backendHttpSettings.Name
            AffinityCookieName = backendHttpSettings.AffinityCookieName
            AuthenticationCertificates = backendHttpSettings.AuthenticationCertificates
            ConnectionDraining = backendHttpSettings.ConnectionDraining |> Option.map ConnectionDrainingConfig.BuildResource
            CookieBasedAffinity = backendHttpSettings.CookieBasedAffinity
            HostName = backendHttpSettings.HostName
            Path = backendHttpSettings.Path
            Port = backendHttpSettings.Port
            Protocol = backendHttpSettings.Protocol
            PickHostNameFromBackendAddress = backendHttpSettings.PickHostNameFromBackendAddress
            RequestTimeoutInSeconds = backendHttpSettings.RequestTimeoutInSeconds
            Probe = backendHttpSettings.Probe
            ProbeEnabled = backendHttpSettings.ProbeEnabled
            TrustedRootCertificates = backendHttpSettings.TrustedRootCertificates
        |}

type BackendHttpSettingsBuilder () =
    member _.Yield _ = 
        {
            Name = ResourceName.Empty
            AffinityCookieName = None
            AuthenticationCertificates = []
            ConnectionDraining = None
            CookieBasedAffinity = FeatureFlag.Disabled
            HostName = None
            Path = None
            Port = 80us
            Protocol = Protocol.Http
            PickHostNameFromBackendAddress = false
            RequestTimeoutInSeconds = 500<Seconds>
            Probe = None // ResourceName.Empty
            ProbeEnabled = false
            TrustedRootCertificates = []
        }
    [<CustomOperation "name">]
    member _.Name (state:BackendHttpSettingsConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "affinity_cookie_name">]
    member _.AffinityCookieName (state:BackendHttpSettingsConfig, name) =
        { state with AffinityCookieName = Some name }
    [<CustomOperation "add_auth_certs">]
    member _.AddAuthCerts (state:BackendHttpSettingsConfig, authCerts) =
        { state with AuthenticationCertificates = state.AuthenticationCertificates @ (authCerts |> List.map (fun authCert -> ResourceName authCert)) }
    [<CustomOperation "connection_draining">]
    member _.ConnectionDraining (state:BackendHttpSettingsConfig, connDraining) =
        { state with ConnectionDraining = Some connDraining }
    [<CustomOperation "cookie_based_affinity">]
    member _.CookieBasedAffinity (state:BackendHttpSettingsConfig, cookieBasedAffinity) =
        { state with CookieBasedAffinity = cookieBasedAffinity }
    [<CustomOperation "host_name">]
    member _.HostName (state:BackendHttpSettingsConfig, name) =
        { state with HostName = Some name }
    [<CustomOperation "path">]
    member _.Path (state:BackendHttpSettingsConfig, path) =
        { state with Path = Some path }
    [<CustomOperation "port">]
    member _.Port (state:BackendHttpSettingsConfig, port) =
        { state with Port = port }
    [<CustomOperation "protocol">]
    member _.Protocol (state:BackendHttpSettingsConfig, protocol) =
        { state with Protocol = protocol }
    [<CustomOperation "pick_host_name_from_backend_address">]
    member _.PickHostNameFromBackendAddress (state:BackendHttpSettingsConfig, pickHostNameFromBackendAddress) =
        { state with PickHostNameFromBackendAddress = pickHostNameFromBackendAddress }
    [<CustomOperation "request_timeout">]
    member _.RequestTimeoutInSeconds (state:BackendHttpSettingsConfig, reqTimeout) =
        { state with RequestTimeoutInSeconds = reqTimeout }
    [<CustomOperation "probe">]
    member _.Probe (state:BackendHttpSettingsConfig, probe:string) =
        { state with Probe = Some (ResourceName probe) }
    [<CustomOperation "probe_enabled">]
    member _.ProbeEnabled (state:BackendHttpSettingsConfig, probeEnabled) =
        { state with ProbeEnabled = probeEnabled }
    [<CustomOperation "trusted_root_certs">]
    member _.TrustedRootCertificates (state:BackendHttpSettingsConfig, trustedRootCerts) =
        { state with TrustedRootCertificates = state.TrustedRootCertificates @ (trustedRootCerts |> List.map (fun rootCert -> ResourceName rootCert)) }
    
let backendHttpSettings = BackendHttpSettingsBuilder()

type HttpListenerCustomErrorConfig =
    {
        CustomErrorPageUrl: string
        StatusCode: HttpStatusCode 
    }
    static member BuildResource customError =
        {|
            CustomErrorPageUrl = customError.CustomErrorPageUrl
            StatusCode = customError.StatusCode
        |}

type HttpListenerCustomErrorBuilder () = 
    member _.Yield _ =
        {
            CustomErrorPageUrl = ""
            StatusCode = HttpStatusCode.HttpStatus502
        }
    [<CustomOperation "page_url">]
    member _.PageUrl (state:HttpListenerCustomErrorConfig, url) = 
        { state with CustomErrorPageUrl = url }
    [<CustomOperation "status_code">]
    member _.StatusCode (state:HttpListenerCustomErrorConfig, code) = 
        { state with StatusCode = code }

let customErrorConfig = HttpListenerCustomErrorBuilder()

type HttpListenerConfig =
    {
        Name: ResourceName
        FrontendIpConfiguration: ResourceName
        BackendAddressPool: ResourceName option
        CustomErrorConfigurations: HttpListenerCustomErrorConfig list
        FirewallPolicy : LinkedResource option
        FrontendPort : ResourceName
        RequireServerNameIndication : bool
        HostNames : string list
        Protocol : Protocol
        SslCertificate : ResourceName option
        SslProfile : ResourceName option
    }
    static member BuildResource httpListener = 
        {|
            Name = httpListener.Name
            FrontendIpConfiguration = httpListener.FrontendIpConfiguration
            BackendAddressPool = httpListener.BackendAddressPool
            CustomErrorConfigurations = httpListener.CustomErrorConfigurations 
            |> List.map (fun conf -> 
                {|
                    CustomErrorPageUrl = conf.CustomErrorPageUrl
                    StatusCode = conf.StatusCode
                |}
            )
            FirewallPolicy = httpListener.FirewallPolicy
            |> Option.map (function | Managed resId -> resId | Unmanaged resId -> resId)
            FrontendPort = httpListener.FrontendPort
            RequireServerNameIndication = httpListener.RequireServerNameIndication
            HostNames = httpListener.HostNames
            Protocol = httpListener.Protocol
            SslCertificate = httpListener.SslCertificate
            SslProfile = httpListener.SslProfile
        |}

type HttpListenerBuilder () =
    member _.Yield _ =
        {
            Name = ResourceName.Empty
            FrontendIpConfiguration = ResourceName.Empty
            BackendAddressPool = None
            CustomErrorConfigurations = []
            FirewallPolicy = None
            FrontendPort = ResourceName.Empty
            RequireServerNameIndication = false
            HostNames = []
            Protocol = Protocol.Http
            SslCertificate = None
            SslProfile = None
        }
    [<CustomOperation "name">]
    member _.Name (state:HttpListenerConfig, name) = 
        { state with Name = ResourceName name }
    [<CustomOperation "frontend_ip_config">]
    member _.FrontendIpConfiguration (state:HttpListenerConfig, ipConfig) = 
        { state with FrontendIpConfiguration = ResourceName ipConfig }
    [<CustomOperation "backened_address_pool">]
    member _.BackendAddressPool (state:HttpListenerConfig, addressPool) = 
        { state with BackendAddressPool = Some (ResourceName addressPool) }
    [<CustomOperation "add_custom_error_config">]
    member _.CustomErrorConfigurations (state:HttpListenerConfig, customErrorConfig) = 
        { state with CustomErrorConfigurations = state.CustomErrorConfigurations @ customErrorConfig }
    [<CustomOperation "link_to_firewall_policy">]
    member _.FirewallPolicy (state:HttpListenerConfig, firewallPolicy) = 
        { state with FirewallPolicy = Some (Unmanaged (azureFirewallPolicies.resourceId (ResourceName firewallPolicy))) }
    [<CustomOperation "frontend_port">]
    member _.FrontendPort (state:HttpListenerConfig, port) = 
        { state with FrontendPort = ResourceName port }
    [<CustomOperation "req_server_name_indication">]
    member _.RequireServerNameIndication (state:HttpListenerConfig, indication) = 
        { state with RequireServerNameIndication = indication }
    [<CustomOperation "add_host_names">]
    member _.HostNames (state:HttpListenerConfig, hostNames) = 
        { state with HostNames = state.HostNames @ hostNames }
    [<CustomOperation "protocol">]
    member _.Protocol (state:HttpListenerConfig, protocol) = 
        { state with Protocol = protocol }
    [<CustomOperation "ssl_cert">]
    member _.SslCertificate (state:HttpListenerConfig, cert) = 
        { state with SslCertificate = Some (ResourceName cert) }
    [<CustomOperation "ssl_profile">]
    member _.SslProfile (state:HttpListenerConfig, sslProfile) = 
        { state with SslProfile = Some (ResourceName sslProfile) }
    
let httpListener = HttpListenerBuilder()

type AppGatewayConfig =
    { Name : ResourceName
      Sku: ApplicationGatewaySku
      GatewayIpConfigs: GatewayIpConfig list
      FrontendIpConfigs: FrontendIpConfig list
      FrontendPorts: FrontendPortConfig list
      BackendAddressPools: BackendAddressPoolConfig list
      BackendHttpSettingsCollection: BackendHttpSettingsConfig list
      HttpListeners: HttpListenerConfig list
      Dependencies: Set<ResourceId>
      Tags: Map<string,string>
     }
    interface IBuilder with
        member this.ResourceId = ApplicationGateways.resourceId this.Name
        member this.BuildResources location =
            let frontendPublicIps =
                this.FrontendIpConfigs
                |> List.map (fun frontend -> FrontendIpConfig.BuildIp frontend location)
                |> List.choose id
            let backendPools =
                this.BackendAddressPools
                |> List.map (fun pool -> { pool with ApplicationGateway = this.Name })
                |> List.map (fun be -> (be :> IBuilder).BuildResources location)
                |> List.concat
            {
                Name = this.Name
                Location = location
                Sku = this.Sku
                GatewayIPConfigurations = this.GatewayIpConfigs |> List.map GatewayIpConfig.BuildResource
                FrontendIpConfigs = this.FrontendIpConfigs |> List.map FrontendIpConfig.BuildResource
                FrontendPorts = this.FrontendPorts |> List.map FrontendPortConfig.BuildResource
                BackendAddressPools = this.BackendAddressPools |> List.map (fun p -> p.Name)
                BackendHttpSettingsCollection = this.BackendHttpSettingsCollection |> List.map BackendHttpSettingsConfig.BuildResource
                HttpListeners = this.HttpListeners |> List.map HttpListenerConfig.BuildResource


                Dependencies =
                    frontendPublicIps
                    |> List.map (fun pip -> publicIPAddresses.resourceId pip.Name)
                    |> Set.ofList
                    |> Set.union this.Dependencies
                Tags = this.Tags

                // TODO Implement properties below
                Identity = Unchecked.defaultof<_>
                AuthenticationCertificates = Unchecked.defaultof<_>
                AutoscaleConfiguration = Unchecked.defaultof<_>
                CustomErrorConfigurations = Unchecked.defaultof<_>
                EnableFips = Unchecked.defaultof<_>
                EnableHttp2 = Unchecked.defaultof<_>
                FirewallPolicy = Unchecked.defaultof<_>
                ForceFirewallPolicyAssociation = Unchecked.defaultof<_>
                // HttpListeners = Unchecked.defaultof<_>
                Probes = Unchecked.defaultof<_>
                RedirectConfigurations = Unchecked.defaultof<_>
                RequestRoutingRules = Unchecked.defaultof<_>
                RewriteRuleSets = Unchecked.defaultof<_>
                SslCertificates = Unchecked.defaultof<_>
                SslPolicy = Unchecked.defaultof<_>
                SslProfiles = Unchecked.defaultof<_>
                TrustedClientCertificates = Unchecked.defaultof<_>
                TrustedRootCertificates = Unchecked.defaultof<_>
                UrlPathMaps = Unchecked.defaultof<_>
                WebApplicationFirewallConfiguration = Unchecked.defaultof<_>
                Zones = Unchecked.defaultof<_>
                
            } :> IArmResource
            :: backendPools
            @ (frontendPublicIps |> Seq.cast<IArmResource> |> List.ofSeq)
            

type AppGatewayBuilder() =
    member _.Yield _ : AppGatewayConfig = {
        Name = ResourceName.Empty
        Sku = {
            Name = Sku.Standard_v2
            Capacity = None
            Tier = Tier.Standard_v2
        }
        GatewayIpConfigs = []
        FrontendIpConfigs = []
        FrontendPorts = []
        BackendAddressPools = []
        BackendHttpSettingsCollection = []
        HttpListeners = []
        Dependencies = Set.empty
        Tags = Map.empty
    }
    [<CustomOperation "name">]
    member _.Name (state:AppGatewayConfig, name) =
        { state with Name = ResourceName name }
    [<CustomOperation "sku">]
    member _.Sku (state:AppGatewayConfig, skuName) = 
        { state with Sku = { state.Sku with Name = skuName }}
    [<CustomOperation "tier">]
    member _.Tier (state:AppGatewayConfig, skuTier) = 
        { state with Sku = { state.Sku with Tier = skuTier }}
    [<CustomOperation "sku_capacity">]
    member _.SkuCapacity (state:AppGatewayConfig, skuCapacity) =
        { state with Sku = { state.Sku with Capacity = Some skuCapacity }}
    [<CustomOperation "add_ip_configs">]
    member _.AddIpConfigs (state:AppGatewayConfig, ipConfigs) =
        { state with GatewayIpConfigs = state.GatewayIpConfigs @ ipConfigs }
    [<CustomOperation "add_frontends">]
    member _.AddFrontends (state:AppGatewayConfig, frontends) =
        { state with FrontendIpConfigs = state.FrontendIpConfigs @ frontends }
    [<CustomOperation "add_frontend_ports">]
    member _.AddFrontendPorts (state:AppGatewayConfig, frontendPorts) =
        { state with FrontendPorts = state.FrontendPorts @ frontendPorts }
    [<CustomOperation "add_backend_address_pools">]
    member _.AddBackendAddresspools (state:AppGatewayConfig, backendAddressPools) =
        { state with BackendAddressPools = state.BackendAddressPools @ backendAddressPools }
    [<CustomOperation "add_backend_https_settings_collection">]
    member _.AddBackendHttpSettingsCollection (state:AppGatewayConfig, backendHttpSettings) = 
        { state with BackendHttpSettingsCollection = state.BackendHttpSettingsCollection @ backendHttpSettings }
    [<CustomOperation "add_http_listeners">]
    member _.AddHttpListeners (state:AppGatewayConfig, httpListeners) =
        { state with HttpListeners = state.HttpListeners @ httpListeners}

let appGateway = AppGatewayBuilder()
