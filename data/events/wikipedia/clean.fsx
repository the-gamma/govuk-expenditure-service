// ------
// Common
// ------
let reader filePath = System.IO.File.ReadAllLines(filePath)

let scrape ((y:string), (sr:System.Collections.Generic.IEnumerable<string>)) (s:string) = 
  Seq.choose (fun (r:string) -> if r.Contains s then Some(y + " " + r) else None) sr

let POLITICALSTRINGS = ["David Cameron"; "Boris Johnson"; "Conservative"; "Labour"; "Politic"; "Government"; "Economic"; "Conservative";]

// // ----
// // 2016
// // ----
// let path2016 = "clean/2016.txt"

// let events2016 = 
//   POLITICALSTRINGS
//   |> Seq.collect (scrape ("2016", (reader path2016)))
//   |> Seq.distinct

// Seq.iter (fun x -> printfn "%s\n" x) events2016 |> ignore

//---------
//All years
//---------
let addReaders root extension years = List.map (fun y -> (y, (reader (root + y + extension)))) years

let yearsAndReaders = addReaders "clean/" ".txt" ["2010"; "2011"; "2012"; "2013"; "2014"; "2015"; "2016"]

let events yearAndReader = 
 POLITICALSTRINGS
 |> Seq.collect (scrape yearAndReader)
 |> Seq.distinct

let allEvents =
  Seq.collect events yearsAndReaders

Seq.iter (fun x -> printfn "%s\n" x) allEvents