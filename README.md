# SharpSearch

## Description

Project to quickly filter through a file share for targeted files for desired information.

## Usage

```
Usage:
    Arguments:
        Required:
            path          - Path to search for files. Note: If using quoted paths, ensure you
                            escape backslashes properly.
        
        Optional:
            pattern       - Type of files to search for, e.g. "*.txt"

            ext_whitelist - Specify file extension whitelist. e.g., ext_whitelist=.txt,.bat,.ps1

            ext_blacklist - Specify file extension blacklist. e.g., ext_blacklist=.zip,.tar,.txt

            searchterms   - Specify a comma deliminated list of searchterms. e.g.searchterms="foo, bar, asdf"

            year          - Filter files by year.


 Examples:
        
        Find all files that have the phrase ""password"" in them.
        
            SharpSearch.exe path="C:\Users\User\Documents\" searchterms=password

        Find all batch and powershell scripts in SYSVOL that were created in 2018 containing the word Administrator

            SharpSearch.exe path="\\DC01\SYSVOL" ext_whitelist=.ps1,.bat searchterms=Administrator year=2018
```

## Examples

- Search for all text files in C:\Users\EXAMPLE\hashcat-4.2.1\wordlists

```
Directory of C:\Users\EXAMPLE\hashcat-4.2.1\wordlists
        12/12/2018 12:00:00 AM       736.78 MB 899_have-i-been-pwned-v3--v2-excluded-_found_hash_plain.txt

               1 Files(s)       736.78 MB

Directory of C:\Users\EXAMPLE\hashcat-4.2.1\wordlists\crackstation-human-only.txt
        9/5/2010 12:00:00 AM       683.25 MB realhuman_phill.txt

               1 Files(s)       683.25 MB

Directory of C:\Users\EXAMPLE\hashcat-4.2.1\wordlists\ProbWL-v2-Real-Passwords-7z\Top2Billion-probable-v2
        2/16/2018 12:00:00 AM        20.25 GB Top2Billion-probable-v2.txt

               1 Files(s)        20.25 GB

        Total Files Listed:
               3 File(s)     21.64 GB
               3 Dir(s)
```
## Special Thanks

Special thanks to [@x3419](https://github.com/x3419) for creating SharperSearch which spurred the revamp to this project.
