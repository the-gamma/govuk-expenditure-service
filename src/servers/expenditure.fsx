#if INTERACTIVE
#I "../../packages"
#r "../../packages/Suave/lib/net40/Suave.dll"
#r "../../packages/Newtonsoft.Json/lib/net40/Newtonsoft.Json.dll"
#r "../../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#load "../common/LoadData.fs" "../common/serializer.fs" "../common/JsonHandler.fs"
#else
module GovUK.Server
#endif
#nowarn "1104"

open Suave
open Suave.Filters
open Suave.Operators
open System
open System.IO
open FSharp.Data
open Newtonsoft.Json
open System.Collections.Generic
open GovUK.Dictionaries
open GovUK.JSON

// ------------------------
// Type stuff for responses
// ------------------------

type ThingSchema = { ``@context``:string; ``type``:string; name:string; }
type GenericType = { name:string; ``params``:obj[] }
type TypePrimitive = { kind:string; ``type``:obj; endpoint:string }
type TypeNested = { kind:string; endpoint:string }
type Member = { name:string; returns:obj; trace:string[]; schema:ThingSchema }
// let makeSchemaThing kind name =
//  { ``@context`` = "http://schema.org/"; ``type`` = kind; name = name }
// let makeSchemaExt kind name =
//  { ``@context`` = "http://thegamma.net/worldbank"; ``type`` = kind; name = name }
let noSchema = Unchecked.defaultof<ThingSchema>

// -----------
// Data import
// -----------

let allData = GovUK.Dictionaries.retrieveData ()
// ------
// Server
// ------

let memberPath s f =
  path s >=> request (fun _ -> f() |> Array.ofSeq |> toJson |> Successful.OK)

let memberPathf fmt f =
  // Tom's easier-to-understand version. If it breaks replace it with Tomas' version, commented out below.
  pathScan fmt (f >> Array.ofSeq >> toJson >> Successful.OK)
  // pathScan fmt (fun b -> f b |> Array.ofSeq |> toJson |> Successful.OK)

let (|Lookup|_|) k (dict:IDictionary<_,_>) =
  match dict.TryGetValue k with
  | true, v -> Some v
  | _ -> None

let app =
  Writers.setHeader  "Access-Control-Allow-Origin" "*"
  >=> Writers.setHeader "Access-Control-Allow-Headers" "content-type,x-cookie"
  >=> choose [
      memberPath "/" (fun () ->
        [ { name="byService"; returns= {kind="nested"; endpoint="/pickService"}
            trace=[| |]; schema = noSchema }
          { name="byYear"; returns= {kind="nested"; endpoint="/pickYear"}
            trace=[| |]; schema = noSchema } ])

      memberPath "/pickYear" (fun () ->
        [ for (KeyValue(id, year)) in allData.Years ->
            { name=year; returns={kind="nested"; endpoint="/byYear/pickSlice"}
              trace=[|"year=" + id |]; schema = noSchema } ])

      memberPath "/byYear/pickSlice" (fun () ->
        [ { name="byAccount"; returns= {kind="nested"; endpoint="/byYear/pickAccount"}
            trace=[| |]; schema = noSchema }
          { name="inTermsOf"; returns= {kind="nested"; endpoint="/byYear/pickTerms"}
            trace=[| |]; schema = noSchema } 
          { name="byService"; returns= {kind="nested"; endpoint="/byYear/pickService"}
            trace=[| |]; schema = noSchema }])

      memberPath "/byYear/pickService" (fun () ->
        let mainServices = GovUK.Dictionaries.OfWhichAreMainServices allData.Services
        [ for (KeyValue(id, (_,_,service))) in mainServices ->
            let typ = { name="tuple"; ``params``=[| "string"; "float" |] }
            let typ = { name="seq"; ``params``=[| typ |]}
            { name=service; returns={ kind="primitive"; ``type``= typ; endpoint="/data"}
              trace=[|"service=" + id |]; schema = noSchema } ])

      memberPath "/pickService" (fun () ->
        let mainServices = GovUK.Dictionaries.OfWhichAreMainServices allData.Services
        [ for (KeyValue(id, (_,_,service))) in mainServices ->
            { name=service; returns={kind="nested"; endpoint="/byService/" + id + "/pickOptions"}
              trace=[| |]; schema = noSchema } ])

      memberPathf "/byService/%s/pickOptions" (fun serviceid ->
            [ { name="bySubService"; returns= {kind="nested"; endpoint="/byService/" + serviceid + "/pickSubService"}
                trace=[| "service=" + serviceid |]; schema = noSchema }
              { name="bySubServiceComponents"; returns= {kind="nested"; endpoint="/byService/" + serviceid + "/pickSubServiceComponents"}
                trace=[| |]; schema = noSchema }
              { name="byAccount"; returns= {kind="nested"; endpoint="/byService/pickAccount"}
                trace=[| "service=" + serviceid |]; schema = noSchema }
              { name="inTermsOf"; returns= {kind="nested"; endpoint="/byService/pickTerms"}
                trace=[| "service=" + serviceid |]; schema = noSchema } 
              { name="ofWhichComponentIs"; returns= {kind="nested"; endpoint="/byService/"+serviceid+"/pickComponents"}
                trace=[| |]; schema = noSchema }])

      memberPathf "/byService/%s/pickSlice" (fun serviceid ->
        [ { name="bySubService"; returns= {kind="nested"; endpoint="/byService/" + serviceid + "/pickSubService"}
            trace=[| |]; schema = noSchema }
          { name="byAccount"; returns= {kind="nested"; endpoint="/byService/pickAccount"}
            trace=[| |]; schema = noSchema }
          { name="inTermsOf"; returns= {kind="nested"; endpoint="/byService/pickTerms"}
            trace=[| |]; schema = noSchema }])

      memberPathf "/byService/%s/pickSubService" (fun serviceid ->
        let childrenOfService = GovUK.Dictionaries.getChildrenWithParentIDAtLevel serviceid "Subservice" allData.SubServices
        [ for (KeyValue(id, (parent, level, subservice))) in childrenOfService ->
            let typ = { name="tuple"; ``params``=[| "int"; "float" |] }
            let typ = { name="seq"; ``params``=[| typ |]}
            { name=subservice; returns={ kind="primitive"; ``type``= typ; endpoint="/data"}
              trace=[|"service=" + id;"level=Subservice"|]; schema = noSchema }])
      
      memberPathf "/byService/%s/pickSubServiceComponents" (fun serviceid ->
        let subservices = GovUK.Dictionaries.getGrandchildrenOfServiceID serviceid allData.SubServices
        [ for (KeyValue(id, (parent, level, service))) in subservices ->
            let typ = { name="tuple"; ``params``=[| "int"; "float" |] }
            let typ = { name="seq"; ``params``=[| typ |]}
            { name=service; returns={ kind="primitive"; ``type``= typ; endpoint="/data"}
              trace=[|"service=" + id;"level=Component of Subservice"|]; schema = noSchema }])
        
      memberPathf "/%s/pickAccount" (fun byX ->
        [ for (KeyValue(id, account)) in allData.Accounts ->
            let typ =
                if byX = "byYear" then { name="tuple"; ``params``=[| "string"; "float" |] }
                elif byX = "byService" then { name="tuple"; ``params``=[| "int"; "float" |] }
                else failwith "bad request"
            let typ = { name="seq"; ``params``=[| typ |]}
            { name=account; returns={ kind="primitive"; ``type``= typ; endpoint="/data"}
              trace=[|"account=" + id |]; schema = noSchema } ])

      memberPathf "/%s/pickTerms" (fun byX ->
        [ for (KeyValue(id, term)) in allData.Terms ->
            let typ =
                if byX = "byYear" then { name="tuple"; ``params``=[| "string"; "float" |] }
                elif byX = "byService" then { name="tuple"; ``params``=[| "int"; "float" |] }
                else failwith "bad request"
            let typ = { name="seq"; ``params``=[| typ |]}
            { name=term; returns={ kind="primitive"; ``type``= typ; endpoint="/data"}
              trace=[|"inTermsOf=" + id |]; schema = noSchema } ])

      memberPathf "/byService/%s/pickComponents" (fun serviceid ->
        let componentsOfService = GovUK.Dictionaries.getChildrenWithParentIDAtLevel serviceid "Component of Service" allData.Services
        [ for (KeyValue(id, (_,_,service))) in componentsOfService ->
            { name=service; returns={kind="nested"; endpoint="/byService/" + id + "/pickSlice"}
              trace=[|"service=" + id;"level=Component of Service"|]; schema = noSchema }  ])

      path "/data" >=> request (fun r ->
        let trace =
          [ for kvps in (Utils.ASCII.toString r.rawForm).Split('&') ->
              match kvps.Split('=') with
              | [| k; v |] -> k, v
              | _ -> failwith "bad  1" ] |> dict
        
        match trace with
        | (Lookup "service" s) & (Lookup "account" a) ->
          allData.Data
            |> Seq.filter (fun dt -> dt.Service = s && dt.Account = a)
            |> Seq.map (fun dt -> allData.Years.[dt.Year], dt.Value)
            |> formatPairSeq JsonValue.String
            |> Successful.OK
        | (Lookup "service" s) & (Lookup "inTermsOf" t) ->
          allData.Data
            |> Seq.filter (fun dt -> dt.Service = s && dt.ValueInTermsOf = t)
            |> Seq.map (fun dt -> allData.Years.[dt.Year], dt.Value)
            |> formatPairSeq JsonValue.String
            |> Successful.OK
        | (Lookup "service" s) & (Lookup "level" a) ->
          allData.Data
            |> Seq.filter (fun dt -> dt.Service = s && dt.Level = a)
            |> Seq.map (fun dt -> allData.Years.[dt.Year], dt.Value)
            |> formatPairSeq JsonValue.String
            |> Successful.OK
        | (Lookup "year" y) & (Lookup "account" a) ->
          allData.Data
            |> Seq.filter (fun dt -> dt.Year = y && dt.Account = a && dt.Level = "Service")
            |> Seq.map (fun dt -> 
              let (parentid, level, service)= allData.Services.[dt.Service]
              (service, dt.Value))
            |> formatPairSeq JsonValue.String
            |> Successful.OK
        | (Lookup "year" y) & (Lookup "inTermsOf" t) ->
          allData.Data
            |> Seq.filter (fun dt -> dt.Year = y && dt.ValueInTermsOf = t && dt.Level = "Service")
            |> Seq.map (fun dt -> 
              let (parentid, level, service)= allData.Services.[dt.Service]
              (service, dt.Value))
            |> formatPairSeq JsonValue.String
            |> Successful.OK
        | (Lookup "year" y) & (Lookup "service" s) ->
          allData.Data 
            |> Seq.filter (fun dt -> dt.Year = y && dt.Parent = s && dt.Level = "Subservice") 
            |> Seq.map (fun dt -> 
              let (parentid, level, serviceName)= allData.SubServices.[dt.Service]
              (serviceName, dt.Value))
            |> formatPairSeq JsonValue.String
            |> Successful.OK
        // | (Lookup "year" y) & (Lookup "service" s) ->
        //   allData.Data 
        //     |> Seq.filter (fun dt -> dt.Year = y && dt.Service = s)
        //     |> Seq.map (fun dt -> 
        //       let (parentid, level, service)= allData.Services.[dt.Parent]
        //       (service, dt.Value))
        //     |> formatPairSeq JsonValue.String
        //     |> Successful.OK
        | _ -> failwith "bad trace")
        // | (Lookup "service" s) & (Lookup "subservice" ss) ->
        //   allData.Data
        //     |> Seq.filter (fun dt -> dt.Service = s && dt.Subservice = ss)
        //     |> Seq.map (fun dt -> allData.Years.[dt.Year], dt.Value)
        //     |> formatPairSeq JsonValue.String
        //     |> Successful.OK
        // | (Lookup "service" s) ->
        //   allData.Data
        //     |> Seq.filter (fun dt -> dt.Service = s)
        //     |> Seq.map (fun dt -> allData.Years.[dt.Year], dt.Value)
        //     |> formatPairSeq JsonValue.String
        //     |> Successful.OK
        // | (Lookup "year" y) & (Lookup "account" a) ->
        //   allData.Data
        //     |> Seq.filter (fun dt -> dt.Year = y && dt.Account = a)
        //     |> Seq.map (fun dt -> allData.Services.[dt.Service], dt.Value)
        //     |> formatPairSeq JsonValue.String
        //     |> Successful.OK
        // | (Lookup "year" y) & (Lookup "inTermsOf" t) ->
        //   allData.Data
        //     |> Seq.filter (fun dt -> dt.Year = y && dt.ValueInTermsOf = t)
        //     |> Seq.map (fun dt -> allData.Services.[dt.Service], dt.Value)
        //     |> formatPairSeq JsonValue.String
        //     |> Successful.OK
        // | (Lookup "year" y) & (Lookup "service" s) ->
        //   allData.Data 
        //     |> Seq.filter (fun dt -> dt.Year = y && dt.Service = s && dt.Subservice <> "")
        //     |> Seq.map (fun dt -> snd(allData.SubServices.[dt.Subservice]), dt.Value)
        //     |> formatPairSeq JsonValue.String
        //     |> Successful.OK
        // | (Lookup "year" y) & (Lookup "service" s) ->
        //   allData.Data 
        //     |> Seq.filter (fun dt -> dt.Year = y && dt.Service = s && dt.Subservice <> "")
        //     |> Seq.map (fun dt -> snd(allData.SubServices.[dt.Subservice]), dt.Value)
        //     |> formatPairSeq JsonValue.String
        //     |> Successful.OK
        // | _ -> failwith traceMessage) 
       ]

