module GovUK.Dictionaries
open System
open System.Collections.Generic
open System.IO
open FSharp.Core
open FSharp.Data

#if INTERACTIVE
let dataDirectory = __SOURCE_DIRECTORY__ + "/../../data/"
#else
let dataDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/../data"
#endif

type DataPoint(service:string, subservice:string, account: string, valueInTermsOf:string, year:string, value:float, parentid: string, level: string) =
     member x.ToString = sprintf "Service: %s, Parent: %s, Value: %f, inTermsOf: %s, Account: %s, Year: %s, Level:%s" service parentid value valueInTermsOf account year level
     member x.Service = service
     member x.Subservice = subservice
     member x.Account = account
     member x.ValueInTermsOf = valueInTermsOf
     member x.Year = year
     member x.Value = value
     member x.Parent = parentid
     member x.Level = level

type Dictionaries =
    {
        Services: IDictionary<string, (string * string * string)>
        SubServices: IDictionary<string, (string * string * string)>
        SubServiceSeq: (string * (string * string * string)) list
        Years:IDictionary<string, string>
        Accounts:IDictionary<string, string>
        Terms:IDictionary<string, string>
        Data : DataPoint list
    }
let [<Literal>] yearsDictCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/years.csv"
let [<Literal>] servicesDictCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/subservices.csv"
// let [<Literal>] servicesCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/Table5-4-1.csv"

let [<Literal>] Y20112015Csv = __SOURCE_DIRECTORY__ + "/../../data/headers/Table5-2.csv" //subservicesCsv
let [<Literal>] Y19992015Csv = __SOURCE_DIRECTORY__ + "/../../data/headers/Table4-2.csv" //oldservicesCsv

type ServiceDictProvider = CsvProvider<servicesDictCsv, Schema = "string,string,string,string">
type YearDictProvider = CsvProvider<yearsDictCsv, Schema = "string,string">
type Y2011ServiceProvider = CsvProvider<Y20112015Csv, Schema = "string, float, float, float, float, float">
type Y1999ServiceProvider = CsvProvider<Y19992015Csv, Schema = "string, float, float, float, float, float, float, float, float, float, float, float, float, float, float, float, float, float">

let servicesPath = dataDirectory + "/headers/services.csv"
let subservicesPath = dataDirectory + "/headers/subservices.csv"
let yearsPath = dataDirectory + "/headers/years.csv"
let termsPath = dataDirectory + "/headers/terms.csv"
let accountsPath = dataDirectory + "/headers/accounts.csv"
let table4Dot2Path = dataDirectory + "/headers/Table4-2.csv"
let table4Dot3Path = dataDirectory + "/headers/Table4-3.csv"
let table4Dot4Path = dataDirectory + "/headers/Table4-4.csv"
let table5Dot4Dot1Path = dataDirectory + "/headers/Table5-4-1.csv"
let table5Dot4Dot2Path = dataDirectory + "/headers/Table5-4-2.csv"
let table5Dot2Path = dataDirectory + "/headers/Table5-2.csv"

let getKeyOfTerm value keyValueList =
    match List.tryFind (fun (k,v) -> (v = value)) keyValueList with
    | Some (key, value) -> key
    | None -> ""

let getKeyOfObject value keyValueList =
    match List.tryFind (fun (k,(parent, level, v)) -> (v = value)) keyValueList with
    | Some (key, _ ) -> key
    | None -> ""

let getLevelOfObject value keyValueList =
    match List.tryFind (fun (k,(parent, level, v)) -> (v = value)) keyValueList with
    | Some (key, (parent, level, v) ) -> level
    | None -> ""

let getParentOfObject value keyValueList =
    match List.tryFind (fun (k,(parent, level, v)) -> (v = value)) keyValueList with
    | Some (key, (parent, level, v) ) -> parent
    | None -> ""

// let isAlphabet (str:string) =
//     let isInAlphabetRange x = (x > 64 && x < 91 ) || (x > 96 && x < 123) || x = 32
//     let inRange = 
//         str
//         |> Seq.map (fun c -> isInAlphabetRange(int c))
//         |> Seq.exists (fun b -> b=true)
//     inRange


    // children |> dict  

// let getParentOfService serviceID aSeq =   
//     let (_,(parentID,_)) = aSeq |> List.find (fun (id, (parentid, serviceDetails)) -> serviceDetails = subserviceName)
//     parentID

let getKeyOfSubService subServiceName keyValueList =
    match List.tryFind (fun (id, (parentid, sserviceName)) -> (sserviceName = subServiceName)) keyValueList with
    | Some (key, value) -> key 
    | None -> ""

let OfWhichAreMainServices serviceDictionary = 
    serviceDictionary |> Seq.filter (fun (KeyValue(index, (parent, level, name))) -> level="Service")  
 
let getChildrenWithParentIDAtLevel parentID itemLevel serviceDictionary =   
    serviceDictionary |> Seq.filter (fun (KeyValue(id, (parentid, level, _))) -> level = itemLevel && parentid = parentID)

let getGrandchildrenOfServiceID serviceID aSeq =
    let total = new Dictionary<string, (string * string * string)>() 
    let children = getChildrenWithParentIDAtLevel serviceID "Subservice" aSeq
    let totalGrandchildren = children |> Seq.map (fun (KeyValue(id, (parentid,level,serviceName))) -> 
        let grandchildrenOfAChild = getChildrenWithParentIDAtLevel id "Component of Subservice" aSeq
        grandchildrenOfAChild |> Seq.iter (fun (KeyValue(id, (parentid,level,name))) -> total.Add(id, (parentid, level, name)))
        ) 
    printfn "AllGrandchildren: %A " totalGrandchildren
    total
    
// let isMainSubService (subServiceSeq) = 
//     let mainSubServices = subServiceSeq |> Seq.filter (fun (id, (parentid, serviceDetails)) -> 
//         not (isAlphabet id))  
//     mainSubServices

let retrieveData () = 
    // -------------------
    // Dictionary creation
    // -------------------
    let servicesCSV = ServiceDictProvider.Load(servicesPath);
    let yearsCSV = YearDictProvider.Load(yearsPath);
    let termsCSV = YearDictProvider.Load(termsPath);
    let accountsCSV = YearDictProvider.Load(accountsPath);
    let subservicesCSV = ServiceDictProvider.Load(subservicesPath);

    let serviceCsvToList (csvfile:ServiceDictProvider) = 
        [for row in csvfile.Rows ->
            (row.Index, (row.ParentIndex, row.Level, row.Name))]

    let yearCsvToList (csvfile:YearDictProvider) = 
        [for row in csvfile.Rows ->
            (row.Index, row.Name)]

    let serviceSeq = serviceCsvToList servicesCSV
    let serviceDict = serviceSeq |> dict
    let subserviceSeq = serviceCsvToList subservicesCSV
    let subserviceDict = subserviceSeq |> dict
    let yearSeq = yearCsvToList yearsCSV
    let yearDict = yearSeq |> dict
    let termSeq = yearCsvToList termsCSV
    let termDict = termSeq |> dict
    let accountSeq = yearCsvToList accountsCSV
    let accountDict = accountSeq |> dict

    // ------------------------------------
    // Data retrieval (for Chapter 4 and 5)
    // ------------------------------------

    let table4dot2 = Y1999ServiceProvider.Load(table4Dot2Path)
    let table4dot3 = Y1999ServiceProvider.Load(table4Dot3Path)
    let table4dot4 = Y1999ServiceProvider.Load(table4Dot4Path)

    let table5dot4dot1 = Y2011ServiceProvider.Load(table5Dot4Dot1Path)
    let table5dot4dot2 = Y2011ServiceProvider.Load(table5Dot4Dot2Path)
    let table5dot2 = Y2011ServiceProvider.Load(table5Dot2Path)

    let getDataServices account term serviceSequence (csvtable:Y2011ServiceProvider)  =
        let keyOfAccount = getKeyOfTerm account accountSeq
        let keyOfTerm = getKeyOfTerm term termSeq
        List.concat
            [ for row in csvtable.Rows ->
                let keyOfService = getKeyOfObject row.Service serviceSequence
                let keyOfSubservice = ""
                let keyOfParent = getParentOfObject row.Service serviceSequence
                let level = getLevelOfObject row.Service serviceSequence
                let dataRows = [row.``2011``; row.``2012``; row.``2013``; row.``2014``; row.``2015``]
                let dataHeaders = csvtable.Headers.Value.[1..] // Hacky bit
                List.mapi (fun i x -> 
                    DataPoint(keyOfService, keyOfSubservice, keyOfAccount, keyOfTerm, dataHeaders.[i], x, keyOfParent, level)) 
                    dataRows ]

    let getOldDataServices account term serviceSequence (csvtable:Y1999ServiceProvider) = 
        let keyOfAccount = getKeyOfTerm account accountSeq
        let keyOfTerm = getKeyOfTerm term termSeq
        List.concat
            [ for row in csvtable.Rows ->
                let keyOfService = getKeyOfObject row.Service serviceSequence
                let keyOfSubservice = ""
                let keyOfParent = getParentOfObject row.Service serviceSequence
                let level = getLevelOfObject row.Service serviceSequence
                let dataRows = [row.``1999``; row.``2000``; row.``2001``; row.``2002``; row.``2003``; row.``2004``; row.``2005``; row.``2006``; row.``2007``; row.``2008``; row.``2009``; row.``2010``; row.``2011``; row.``2012``; row.``2013``; row.``2014``; row.``2015``]
                let dataHeaders = csvtable.Headers.Value.[1..] // Hacky bit
                List.mapi (fun i x -> 
                    DataPoint(keyOfService, keyOfSubservice, keyOfAccount, keyOfTerm, dataHeaders.[i], x, keyOfParent, level)) 
                    dataRows ]

    let nominalData = getOldDataServices "" "Nominal" serviceSeq table4dot2
    let adjustedData = getOldDataServices "" "Adjusted" serviceSeq table4dot3
    let gdpData = getOldDataServices "" "GDP" serviceSeq table4dot4
    let currentData = getDataServices "Current" "" serviceSeq table5dot4dot1
    let capitalData = getDataServices "Capital" "" serviceSeq table5dot4dot2
    let subserviceData = getDataServices "" "Nominal" subserviceSeq table5dot2 

    let allValues = List.concat [nominalData; adjustedData; gdpData; currentData; capitalData; subserviceData]

    // nominalData |> List.iter (fun x -> printfn "%s" x.ToString)
    // allValues |> Seq.filter (fun dt -> dt.Year = "2015" && dt.Parent = "4.0" && dt.Level = "Subservice") 
    //         |> Seq.iter (fun x -> printfn "%s" x.ToString)
    
    // let (parentid, level, serviceName) = subserviceDict.["4.1"]
    // printfn "Subservice 4.1: %s" serviceName
    { 
        Services=serviceDict;
        SubServices=subserviceDict;
        SubServiceSeq=subserviceSeq;
        Years=yearDict;
        Accounts=accountDict;
        Terms=termDict;
        Data=allValues;
    }