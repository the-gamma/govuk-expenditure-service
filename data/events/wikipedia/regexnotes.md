### Cleaning procedure
- Get from wikipedia
- Delete everything up to `==Events==` (e.g. Replace `[\s\S]*==Events==` with `==Events==`)
- Delete everything after `==See also==` inclusive. (e.g. Delete `==See More==[\s\S]*`)
- Delete `\n\[\[File:.*?$`
- Replace `\[\[[^\]]*?\|(.*?)\]\]` with the captured group ($1)
- Replace `\[\[(.*?)\]\]` with the captured group ($1)
- Delete `<ref[\S\s]*?(/>|</ref>)`
- Replace `–\s*?\n\*\*[\s?]*` with `– `
- Replace `\s*?\n\*\*[\s?]*` with ` – `
- Delete `^\s*?\*[\s?]*`

### Notes on the notes:
- We ignore the stuff between curly brackets. It's way, way, way too hard to parse. It might just be worth deleting, it causes particular problems for 2010...
- There are obviously small mistakes due to mistakes and inconsistencies in the original files.
- You could also get rid of empty lines and lines starting with `===`` if you wanted. I like them as they make the file easier to read.
