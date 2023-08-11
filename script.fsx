#r "nuget: HtmlAgilityPack"
#r "nuget: ExCSS"

open System.Collections.Generic
open System.IO
open System.IO.Compression
open HtmlAgilityPack
open ExCSS

let edit_css (html : string) =
    let parser = StylesheetParser()
    let sheet = parser.Parse(html)
    for rule in sheet.StyleRules do
        if rule.SelectorText = ".c5" then
            rule.Style.MaxWidth <- "1080pt"
            rule.Style.Padding <- null
    sheet.ToCss()
    
let edit_html_styles html =
    let html_doc = HtmlDocument()
    html_doc.LoadHtml(html)

    let headNode = html_doc.DocumentNode.SelectSingleNode("//head")
    let styleNode = headNode.SelectSingleNode("//style")
    styleNode.InnerHtml <- edit_css styleNode.InnerHtml

    html_doc.DocumentNode.OuterHtml
    
let iterate_entries f (archive : ZipArchive) =
    for entry in archive.Entries do
        if Path.GetExtension entry.FullName = ".html" then
            let html : string =
                use stream = entry.Open()
                use reader = new StreamReader(stream)
                reader.ReadToEnd() |> f
                
            use stream = entry.Open()
            use writer = new StreamWriter(stream)
            writer.Write(html)
           
let edit_html_images (d : Dictionary<_,_>) html =
    let html_doc = HtmlDocument()
    html_doc.LoadHtml(html)
    
    let count =
        let mutable i = d.Count
        fun () -> let x = i in i <- i+1; x.ToString("D6") // 6 decimal places
    
    let images = html_doc.DocumentNode.SelectNodes("//img")
    for x in images do
        let path = x.GetAttributeValue("src","")
        let ext = Path.GetExtension(path)
        let path_new = $"images/img{count()}{ext}"
        d.Add(path, path_new)
        x.SetAttributeValue("src", path_new) |> ignore
    
    html_doc.DocumentNode.OuterHtml
    
let rename_image_entries (d : Dictionary<_,_>) (archive : ZipArchive) =
    for KeyValue(path,path_new) in d do
        let entry = archive.GetEntry(path)
        let entry_new = archive.CreateEntry(path_new)
        let _ =
            use a = entry.Open()
            use b = entry_new.Open()
            a.CopyTo(b)
        entry.Delete()
        
let edit_all zip_path =
    let d = Dictionary()
    
    use archive = ZipFile.Open(zip_path, ZipArchiveMode.Update)
    iterate_entries edit_html_styles archive
    iterate_entries (edit_html_images d) archive
    rename_image_entries d archive
    
// This is just a slight change compared to the video. Instead of passing in the file directly, it would be more ergonomic to
// just modify all the zip files in the current folder.
for file in Directory.GetFiles(".","*.zip") do
    printfn "Modifying: %s" file
    edit_all file
    
printfn "Done!"