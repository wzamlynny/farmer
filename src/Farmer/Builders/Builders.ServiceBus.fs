[<AutoOpen>]
module Farmer.Builders.ServiceBus

open Farmer
open Farmer.ServiceBus
open Farmer.Arm.ServiceBus
open Namespaces
open Topics
open System

type ServiceBusQueueConfig =
    { Name : ResourceName
      LockDuration : TimeSpan option
      DuplicateDetection : TimeSpan option
      DefaultMessageTimeToLive : TimeSpan option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option
      MaxDeliveryCount : int option
      MaxSizeInMegabytes : int<Mb> option
      EnablePartitioning : bool option
      AuthorizationRules : Map<ResourceName, AuthorizationRuleRight Set>}

type ServiceBusQueueBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          LockDuration = None
          DuplicateDetection = None
          Session = None
          DeadLetteringOnMessageExpiration = None
          DefaultMessageTimeToLive = None
          MaxDeliveryCount = None
          MaxSizeInMegabytes = None
          EnablePartitioning = None
          AuthorizationRules = Map.empty }

    /// The name of the queue.
    [<CustomOperation "name">] member _.Name(state:ServiceBusQueueConfig, name) = { state with Name = ResourceName name }
    /// The length of time that a lock can be held on a message.
    [<CustomOperation "lock_duration_minutes">] member _.LockDurationMinutes(state:ServiceBusQueueConfig, duration) = { state with LockDuration = Some (TimeSpan.FromMinutes (float duration)) }
    /// The maximum number of times a message can be delivered before dead lettering.
    [<CustomOperation "duplicate_detection_minutes">] member _.DuplicateDetection(state:ServiceBusQueueConfig, maxTimeWindow) = { state with DuplicateDetection = Some (TimeSpan.FromMinutes (float maxTimeWindow)) }
    /// The maximum size for the queue in megabytes.
    [<CustomOperation "max_queue_size">]
    member _.MaxTopicSize(state:ServiceBusQueueConfig, maxTopicSize:int<Mb>) = { state with MaxSizeInMegabytes = Some maxTopicSize }
    /// The default time-to-live for messages in a timespan string (e.g. '00:05:00'). If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl">]
    member _.MessageTtl(state:ServiceBusQueueConfig, ttl) = { state with DefaultMessageTimeToLive = Some (TimeSpan.Parse ttl) }
    /// The default time-to-live for messages in days. If not specified, the maximum TTL will be set for the SKU.
    member _.MessageTtl(state:ServiceBusQueueConfig, ttl:int<Days>) = { state with DefaultMessageTimeToLive = ttl / 1<Days> |> float |> TimeSpan.FromDays |> Some }
    //// The default time-to-live for messages defined in a .NET TimeSpan.
    member _.MessageTtl(state:ServiceBusQueueConfig, timespan:TimeSpan) = { state with DefaultMessageTimeToLive = Some timespan }
    /// Enables session support.
    [<CustomOperation "max_delivery_count">] member _.MaxDeliveryCount(state:ServiceBusQueueConfig, count) = { state with MaxDeliveryCount = Some count }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "enable_session">] member _.Session(state:ServiceBusQueueConfig) = { state with Session = Some true }
    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">] member _.DeadLetteringOnMessageExpiration(state:ServiceBusQueueConfig) = { state with DeadLetteringOnMessageExpiration = Some true }
    /// Enables partition support on the queue.
    [<CustomOperation "enable_partition">] member _.EnablePartition(state:ServiceBusQueueConfig) = { state with EnablePartitioning = Some true }
    /// Add authorization rule on the queue.
    [<CustomOperation "add_authorization_rule">]
    member _.AddAuthorizationRule(state:ServiceBusQueueConfig, name, rights) = { state with AuthorizationRules = state.AuthorizationRules.Add(ResourceName name, Set rights) }

type ServiceBusSubscriptionConfig =
    { Name : ResourceName

      LockDuration : TimeSpan option
      DuplicateDetection : TimeSpan option
      DefaultMessageTimeToLive : TimeSpan option
      ForwardTo : ResourceName option
      MaxDeliveryCount : int option
      Session : bool option
      DeadLetteringOnMessageExpiration : bool option
      Rules : Rule list }

type ServiceBusSubscriptionBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          LockDuration = None
          DuplicateDetection = None
          DefaultMessageTimeToLive = None
          ForwardTo = None
          MaxDeliveryCount = None
          Session = None
          DeadLetteringOnMessageExpiration = None
          Rules = List.empty }

    /// The name of the queue.
    [<CustomOperation "name">]
     member _.Name(state:ServiceBusSubscriptionConfig, name) = { state with Name = ResourceName name }
    /// The length of time that a lock can be held on a message.
    [<CustomOperation "lock_duration_minutes">]
     member _.LockDurationMinutes(state:ServiceBusSubscriptionConfig, duration) = { state with LockDuration = Some (TimeSpan.FromMinutes (float duration)) }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection_minutes">]
     member _.DuplicateDetection(state:ServiceBusSubscriptionConfig, maxTimeWindow) = { state with DuplicateDetection = Some (TimeSpan.FromMinutes (float maxTimeWindow)) }
    /// The default time-to-live for messages in a timespan string (e.g. '00:05:00'). If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl">]
    member _.MessageTtl(state:ServiceBusSubscriptionConfig, ttl) = { state with DefaultMessageTimeToLive = Some (TimeSpan.Parse ttl) }
    /// The default time-to-live for messages in days. If not specified, the maximum TTL will be set for the SKU.
    member _.MessageTtl(state:ServiceBusSubscriptionConfig, ttl:int<Days>) = { state with DefaultMessageTimeToLive = ttl / 1<Days> |> float |> TimeSpan.FromDays |> Some }
    //// The default time-to-live for messages defined in a .NET TimeSpan.
    member _.MessageTtl(state:ServiceBusSubscriptionConfig, timespan:TimeSpan) = { state with DefaultMessageTimeToLive = Some timespan }
    /// Enables session support.
    [<CustomOperation "max_delivery_count">]
     member _.MaxDeliveryCount(state:ServiceBusSubscriptionConfig, count) = { state with MaxDeliveryCount = Some count }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "enable_session">]
     member _.Session(state:ServiceBusSubscriptionConfig) = { state with Session = Some true }
    /// Enables dead lettering of messages that expire.
    [<CustomOperation "enable_dead_letter_on_message_expiration">]
    member _.DeadLetteringOnMessageExpiration(state:ServiceBusSubscriptionConfig) = { state with DeadLetteringOnMessageExpiration = Some true }
    /// Automatically forward to a queue or topic
    [<CustomOperation "forward_to">]
    member _.ForwardTo(state:ServiceBusSubscriptionConfig, target) = { state with ForwardTo = Some (ResourceName target) }
    /// Adds filtering rules for a subscription
    [<CustomOperation "add_filters">]
    member _.AddFilters(state:ServiceBusSubscriptionConfig, filters) = { state with Rules = state.Rules @ filters }
    /// Adds a sql filtering rule for a subscription
    [<CustomOperation "add_sql_filter">]
    member this.AddFilter(state:ServiceBusSubscriptionConfig, name, expression) = this.AddFilters(state, [ Rule.CreateSqlFilter(name, expression) ])
    /// Adds a correlation filtering rule for a subscription
    [<CustomOperation "add_correlation_filter">]
    member this.AddCorrelationFilter(state:ServiceBusSubscriptionConfig, name, properties) = this.AddFilters(state, [ Rule.CreateCorrelationFilter(name, properties) ])


type ServiceBusTopicConfig =
    { Name : ResourceName
      Namespace : LinkedResource
      DuplicateDetection : TimeSpan option
      DefaultMessageTimeToLive : TimeSpan option
      EnablePartitioning : bool option
      MaxSizeInMegabytes : int<Mb> option
      Subscriptions : Map<ResourceName, ServiceBusSubscriptionConfig> }
    interface IBuilder with
        member this.ResourceId = topics.resourceId (this.Namespace.Name, this.Name)
        member this.BuildResources location = [
            { Name = this.Name
              Dependencies = [
                match this.Namespace with
                | Managed resId -> resId // Only generate dependency if this is managed by Farmer (same template)
                | _ -> ()
              ] |> Set.ofList
              Namespace =
                match this.Namespace with
                | Managed resId
                | Unmanaged resId -> resId
              DuplicateDetectionHistoryTimeWindow = this.DuplicateDetection |> Option.map IsoDateTime.OfTimeSpan
              DefaultMessageTimeToLive = this.DefaultMessageTimeToLive |> Option.map IsoDateTime.OfTimeSpan
              EnablePartitioning = this.EnablePartitioning
              MaxSizeInMegabytes = this.MaxSizeInMegabytes }
            for subscription in this.Subscriptions do
                let subscription = subscription.Value
                { Name = subscription.Name
                  Namespace =
                        match this.Namespace with
                        | Managed resId
                        | Unmanaged resId -> resId.Name
                  Topic = this.Name
                  LockDuration = subscription.LockDuration |> Option.map IsoDateTime.OfTimeSpan
                  DuplicateDetectionHistoryTimeWindow = subscription.DuplicateDetection |> Option.map IsoDateTime.OfTimeSpan
                  DefaultMessageTimeToLive = subscription.DefaultMessageTimeToLive |> Option.map IsoDateTime.OfTimeSpan
                  ForwardTo = subscription.ForwardTo
                  MaxDeliveryCount = subscription.MaxDeliveryCount
                  Session = subscription.Session
                  DeadLetteringOnMessageExpiration = subscription.DeadLetteringOnMessageExpiration
                  Rules = subscription.Rules }
        ]

type ServiceBusTopicBuilder() =
    member _.Yield _ =
        { Name = ResourceName.Empty
          Namespace = Managed (namespaces.resourceId ResourceName.Empty)
          DuplicateDetection = None
          DefaultMessageTimeToLive = None
          EnablePartitioning = None
          MaxSizeInMegabytes = None
          Subscriptions = Map.empty }

    /// The name of the queue.
    [<CustomOperation "name">] member _.Name(state:ServiceBusTopicConfig, name) = { state with Name = ResourceName name }
    /// Whether to enable duplicate detection, and if so, how long to check for.ServiceBusQueueConfig
    [<CustomOperation "duplicate_detection_minutes">] member _.DuplicateDetection(state:ServiceBusTopicConfig, maxTimeWindow) = { state with DuplicateDetection = Some (TimeSpan.FromMinutes (float maxTimeWindow)) }
    /// The maximum size for the topic in megabytes.
    [<CustomOperation "max_topic_size">]
    member _.MaxTopicSize(state:ServiceBusTopicConfig, maxTopicSize:int<Mb>) = { state with MaxSizeInMegabytes = Some maxTopicSize }
    /// The default time-to-live for messages in a timespan string (e.g. '00:05:00'). If not specified, the maximum TTL will be set for the SKU.
    [<CustomOperation "message_ttl">]
    member _.MessageTtl(state:ServiceBusTopicConfig, ttl) = { state with DefaultMessageTimeToLive = Some (TimeSpan.Parse ttl) }
    /// The default time-to-live for messages in days. If not specified, the maximum TTL will be set for the SKU.
    member _.MessageTtl(state:ServiceBusTopicConfig, ttl:int<Days>) = { state with DefaultMessageTimeToLive = ttl / 1<Days> |> float |> TimeSpan.FromDays |> Some }
    //// The default time-to-live for messages defined in a .NET TimeSpan.
    member _.MessageTtl(state:ServiceBusTopicConfig, timespan:TimeSpan) = { state with DefaultMessageTimeToLive = Some timespan }
    /// Enables partition support on the topic.
    [<CustomOperation "enable_partition">] member _.EnablePartition(state:ServiceBusTopicConfig) = { state with EnablePartitioning = Some true }
    [<CustomOperation "add_subscriptions">]
    member _.AddSubscriptions(state:ServiceBusTopicConfig, subscriptions) =
        { state with
            Subscriptions =
                (state.Subscriptions, subscriptions)
                ||> List.fold(fun state (subscription:ServiceBusSubscriptionConfig) -> state.Add(subscription.Name, subscription))
        }
    /// Instead of creating or modifying a namespace, configure this topic to point to another unmanaged namespace instance.
    [<CustomOperation "link_to_unmanaged_namespace">]
    member this.LinkToUnmanagedNamespace (state:ServiceBusTopicConfig, namespaceName:ResourceName) =
        { state with Namespace = Unmanaged(namespaces.resourceId namespaceName) }
    member this.LinkToUnmanagedNamespace (state:ServiceBusTopicConfig, namespaceName) =
        { state with Namespace = Unmanaged(namespaces.resourceId(ResourceName namespaceName)) }


type ServiceBusConfig =
    { Name : ResourceName
      Sku : Sku
      Dependencies : ResourceId Set
      Queues : Map<ResourceName, ServiceBusQueueConfig>
      Topics : Map<ResourceName, ServiceBusTopicConfig>
      AuthorizationRules : Map<ResourceName, AuthorizationRuleRight Set>
      Tags: Map<string,string>  }
    member private this.GetKeyPath property =
        let expr = $"listkeys(resourceId('Microsoft.ServiceBus/namespaces/authorizationRules', '{this.Name.Value}', 'RootManageSharedAccessKey'), '2017-04-01').{property}"
        ArmExpression.create(expr, namespaces.resourceId this.Name)
    member this.NamespaceDefaultConnectionString = this.GetKeyPath "primaryConnectionString"
    member this.DefaultSharedAccessPolicyPrimaryKey = this.GetKeyPath "primaryKey"
    interface IBuilder with
        member this.ResourceId = namespaces.resourceId this.Name
        member this.BuildResources location = [
            { Name = this.Name
              Location = location
              Sku = this.Sku
              Dependencies = this.Dependencies
              Tags = this.Tags  }

            for queue in this.Queues do
              let queue = queue.Value
              { Name = queue.Name
                Namespace = this.Name
                LockDuration = queue.LockDuration |> Option.map IsoDateTime.OfTimeSpan
                DuplicateDetectionHistoryTimeWindow = queue.DuplicateDetection |> Option.map IsoDateTime.OfTimeSpan
                Session = queue.Session
                DeadLetteringOnMessageExpiration = queue.DeadLetteringOnMessageExpiration
                DefaultMessageTimeToLive =
                    match queue.DefaultMessageTimeToLive, this.Sku with
                    | None, Basic -> TimeSpan.FromDays 14.
                    | None, (Standard | Premium _) -> TimeSpan.MaxValue
                    | Some ttl, _ -> ttl
                    |> IsoDateTime.OfTimeSpan
                MaxDeliveryCount = queue.MaxDeliveryCount
                MaxSizeInMegabytes = queue.MaxSizeInMegabytes
                EnablePartitioning = queue.EnablePartitioning }
              for rule in queue.AuthorizationRules do
                { QueueAuthorizationRule.Name = rule.Key.Map(fun rule -> $"{this.Name.Value}/{queue.Name.Value}/%s{rule}")
                  Location = location
                  Dependencies = [
                    namespaces.resourceId this.Name
                    queues.resourceId (this.Name, queue.Name)
                  ]
                  Rights = rule.Value }


            for topic in this.Topics do
                let topic = {topic.Value with Namespace = Managed(namespaces.resourceId this.Name)} :> IBuilder
                for topicResource in topic.BuildResources location do
                    topicResource

            for rule in this.AuthorizationRules do
              { Name = rule.Key.Map(fun rule -> $"{this.Name.Value}/%s{rule}")
                Location = location
                Dependencies = [
                  namespaces.resourceId this.Name
                ]
                Rights = rule.Value }
        ]

type ServiceBusBuilder() =
    interface IDependable<ServiceBusConfig> with member _.Add state newDeps = { state with Dependencies = state.Dependencies + newDeps }
    member _.Yield _ =
        { Name = ResourceName.Empty
          Sku = Basic
          Queues = Map.empty
          Topics = Map.empty
          Dependencies = Set.empty
          AuthorizationRules = Map.empty
          Tags = Map.empty }
    member _.Run (state:ServiceBusConfig) =
        let isBetween min max v = v >= min && v <= max
        for queue in state.Queues do
            let queue = queue.Value

            match queue.DuplicateDetection, state.Sku with
            | Some _, Basic -> raiseFarmer $"Duplicate Detection cannot be set when creating a queue using the Basic tier (queue '{queue.Name.Value}' fails this check)."
            | _ -> ()
            queue.LockDuration |> Option.iter(fun lockDuration -> if lockDuration > TimeSpan(0,5,0) then raiseFarmer "Lock duration name must not be more than 5 minutes.")

        state
    /// The name of the namespace that holds the queue.
    [<CustomOperation "name">]
    member _.NamespaceName(state:ServiceBusConfig, name) = { state with Name = ServiceBusValidation.ServiceBusName.Create(name).OkValue.ResourceName }
    /// The SKU of the namespace.
    [<CustomOperation "sku">]
    member _.Sku(state:ServiceBusConfig, sku) = { state with Sku = sku }

    [<CustomOperation "add_queues">]
    member _.AddQueues(state:ServiceBusConfig, queues) =
        { state with
            Queues =
                (state.Queues, queues)
                ||> List.fold(fun state (queue:ServiceBusQueueConfig) -> state.Add(queue.Name, queue))
        }
    [<CustomOperation "add_topics">]
    member _.AddTopics(state:ServiceBusConfig, topics) =
        { state with
            Topics =
                (state.Topics, topics)
                ||> List.fold(fun topics (topic:ServiceBusTopicConfig) -> topics.Add(topic.Name, {topic with Namespace = Managed(namespaces.resourceId state.Name)}))
        }
    /// Add authorization rule on the namespace.
    [<CustomOperation "add_authorization_rule">]
    member _.AddAuthorizationRule(state:ServiceBusConfig, name, rights) = { state with AuthorizationRules = state.AuthorizationRules.Add(ResourceName name, Set rights) }
    interface ITaggable<ServiceBusConfig> with member _.Add state tags = { state with Tags = state.Tags |> Map.merge tags }

let serviceBus = ServiceBusBuilder()
let topic = ServiceBusTopicBuilder()
let queue = ServiceBusQueueBuilder()
let subscription = ServiceBusSubscriptionBuilder()
