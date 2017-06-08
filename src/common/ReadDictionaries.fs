module GovUK.DictionariesOld

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

type DataPoint(service:string, subservice:string, account: string, valueInTermsOf:string, year:string, value:float) =
     member x.ToString = sprintf "Service: %s, Subservice: %s, Value: %f, inTermsOf: %s, Account: %s, Year: %s," service subservice value valueInTermsOf account year
     member x.Service = service
     member x.Subservice = subservice
     member x.Account = account
     member x.ValueInTermsOf = valueInTermsOf
     member x.Year = year
     member x.Value = value


type Dictionaries =
    {
        Services: IDictionary<string, string>
        SubServices: IDictionary<string, (string * string)>
        SubServiceSeq: (string * (string * string)) list
        Years:IDictionary<string, string>
        Accounts:IDictionary<string, string>
        Terms:IDictionary<string, string>
        Data : DataPoint list
    }
let [<Literal>] servicesDictCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/services.csv"
let [<Literal>] subservicesDictCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/subservices.csv"
let [<Literal>] servicesCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/Table5-4-1.csv"
let [<Literal>] subservicesCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/Table5-2.csv"
let [<Literal>] oldservicesCsv = __SOURCE_DIRECTORY__ + "/../../data/headers/Table4-2.csv"

type ServiceDictProvider = CsvProvider<servicesDictCsv, Schema = "string,string">
type SubserviceDictProvider = CsvProvider<subservicesDictCsv, Schema = "string,string,string">

type ServiceProvider = CsvProvider<servicesCsv, Schema = "string, float, float, float, float, float">
type SubserviceProvider = CsvProvider<subservicesCsv, Schema = "string, float, float, float, float, float">
type OldServiceProvider = CsvProvider<oldservicesCsv, Schema = "string, float, float, float, float, float, float, float, float, float, float, float, float, float, float, float, float, float">

// module ReadDictionaries =

    // ----------
    // File paths
    // ----------

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

let getKeyOf value keyValueList =
    match List.tryFind (fun (k,v) -> (v = value)) keyValueList with
    | Some (key, value) -> key
    | None -> ""

let getChildrenOfServiceIDOld parentID subserviceSeq =   
    let children = subserviceSeq |> List.filter (fun (id, (parentid, serviceDetails)) -> parentid = parentID)
    children |> dict

let isAlphabet (str:string) =
    let isInAlphabetRange x = (x > 64 && x < 91 ) || (x > 96 && x < 123) || x = 32
    let inRange = 
        str
        |> Seq.map (fun c -> isInAlphabetRange(int c))
        |> Seq.exists (fun b -> b=true)
    inRange

let getChildrenOfServiceID parentID subserviceSeq =   
    let children = subserviceSeq |> List.filter (fun (id, (parentid, serviceDetails)) -> (parentid = parentID) && not (isAlphabet id))
    children |> dict  

let getParentOfSubservice subserviceName subserviceSeq =   
    let (_,(parentID,_))= subserviceSeq |> List.find (fun (id, (parentid, serviceDetails)) -> serviceDetails = subserviceName)
    parentID

let getKeyOfSubService subServiceName keyValueList =
    match List.tryFind (fun (id, (parentid, sserviceName)) -> (sserviceName = subServiceName)) keyValueList with
    | Some (key, value) -> key 
    | None -> ""

let OfWhichAreComponentServices (parentIndex:string, serviceDictionary) = 
    let firstSectionOfID = parentIndex.IndexOf(".")
    let commonID = parentIndex.[0..firstSectionOfID]
    serviceDictionary |> Seq.filter (fun (KeyValue(index, service)) -> index.ToString().StartsWith commonID) 
                        |> Seq.filter (fun (KeyValue(index, service)) -> not (index.ToString().Contains "0") 
                        ) 
let OfWhichAreMainServices (serviceDictionary) = 
    let mainServices = serviceDictionary |> Seq.filter (fun (KeyValue(index, service)) -> index.ToString().Contains ".0")  
    mainServices

let isMainSubService (subServiceSeq) = 
    let mainSubServices = subServiceSeq |> Seq.filter (fun (id, (parentid, serviceDetails)) -> 
        not (isAlphabet id))  
    mainSubServices


let retrieveData () = 
    // -------------------
    // Dictionary creation
    // -------------------
    let servicesCSV = ServiceDictProvider.Load(servicesPath);
    let yearsCSV = ServiceDictProvider.Load(yearsPath);
    let termsCSV = ServiceDictProvider.Load(termsPath);
    let accountsCSV = ServiceDictProvider.Load(accountsPath);
    let subservicesCSV = SubserviceDictProvider.Load(subservicesPath);

    let csvToList (csvfile:ServiceDictProvider) = 
        [for row in csvfile.Rows ->
            (row.Index, row.Name)]

    let subserviceCsvToList (csvfile:SubserviceDictProvider) = 
        [for row in csvfile.Rows ->
            (row.Index, (row.ParentIndex, row.Name))]

    let serviceSeq = csvToList servicesCSV
    let serviceDict = serviceSeq |> dict
    let subserviceSeq = subserviceCsvToList subservicesCSV
    let subserviceDict = subserviceSeq |> dict
    let yearSeq = csvToList yearsCSV
    let yearDict = yearSeq |> dict
    let termSeq = csvToList termsCSV
    let termDict = termSeq |> dict
    let accountSeq = csvToList accountsCSV
    let accountDict = accountSeq |> dict

    // ------------------------------------
    // Data retrieval (for Chapter 4 and 5)
    // ------------------------------------

    let table4dot2 = OldServiceProvider.Load(table4Dot2Path)
    let table4dot3 = OldServiceProvider.Load(table4Dot3Path)
    let table4dot4 = OldServiceProvider.Load(table4Dot4Path)

    let table5dot4dot1 = ServiceProvider.Load(table5Dot4Dot1Path)
    let table5dot4dot2 = ServiceProvider.Load(table5Dot4Dot2Path)

    let table5dot2 = SubserviceProvider.Load(table5Dot2Path)

    let getDataServices account term (csvtable:ServiceProvider)  =
        let keyOfAccount = getKeyOf account accountSeq
        let keyOfTerm = getKeyOf term termSeq
        List.concat
            [ for row in csvtable.Rows ->
                let keyOfService = getKeyOf row.Service serviceSeq
                let keyOfSubservice = ""
                let dataRows = [row.``2011``; row.``2012``; row.``2013``; row.``2014``; row.``2015``]
                let dataHeaders = csvtable.Headers.Value.[1..] // Hacky bit
                List.mapi (fun i x -> 
                    DataPoint(keyOfService, keyOfSubservice, keyOfAccount, keyOfTerm, dataHeaders.[i], x)) 
                    dataRows ]

    let getDataSubservices (csvtable:SubserviceProvider) = 
        let keyOfAccount = ""
        let keyOfTerm = ""
        List.concat
            [ for row in csvtable.Rows ->
                let keyOfService = getParentOfSubservice row.Subservice subserviceSeq
                let keyOfSubservice = getKeyOfSubService row.Subservice subserviceSeq
                let dataRows = [row.``2011``; row.``2012``; row.``2013``; row.``2014``; row.``2015``]
                let dataHeaders = csvtable.Headers.Value.[1..] // Hacky bit
                List.mapi (fun i x -> 
                    DataPoint(keyOfService, keyOfSubservice, keyOfAccount, keyOfTerm, dataHeaders.[i], x)) 
                    dataRows ]

    let getOldDataServices account term (csvtable:OldServiceProvider) = 
        let keyOfAccount = getKeyOf account accountSeq
        let keyOfTerm = getKeyOf term termSeq
        List.concat
            [ for row in csvtable.Rows ->
                let keyOfService = getKeyOf row.Service serviceSeq
                let keyOfSubservice = ""
                let dataRows = [row.``1999``; row.``2000``; row.``2001``; row.``2002``; row.``2003``; row.``2004``; row.``2005``; row.``2006``; row.``2007``; row.``2008``; row.``2009``; row.``2010``; row.``2011``; row.``2012``; row.``2013``; row.``2014``; row.``2015``]
                let dataHeaders = csvtable.Headers.Value.[1..] // Hacky bit
                List.mapi (fun i x -> 
                    DataPoint(keyOfService, keyOfSubservice, keyOfAccount, keyOfTerm, dataHeaders.[i], x)) 
                    dataRows ]

    let nominalData = getOldDataServices "" "Nominal" table4dot2
    let adjustedData = getOldDataServices "" "Adjusted" table4dot3
    let gdpData = getOldDataServices "" "GDP" table4dot4
    let currentData = getDataServices "Current" "" table5dot4dot1
    let capitalData = getDataServices "Capital" "" table5dot4dot2
    let subserviceData = getDataSubservices table5dot2 

    let allValues = List.concat [nominalData; adjustedData; gdpData; currentData; capitalData; subserviceData]

    // printfn "%A" allValues
    
    { 
        Services=serviceDict;
        SubServices=subserviceDict;
        SubServiceSeq=subserviceSeq;
        Years=yearDict;
        Accounts=accountDict;
        Terms=termDict;
        Data=allValues;
    }