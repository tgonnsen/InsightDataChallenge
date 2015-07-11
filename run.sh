#!/usr/bin/env bash

# Use Mono's xbuild utility to compile the project, then Mono to run the application
cd src
xbuild /p:Configuration=Release TweetAnalyzer.csproj
mono TweetAnalyzer.exe

# For Linux systems, need to concatenate files and cleanup temp files
cd ../tweet_output
cat temp*.txt > ft1.txt
rm temp*.txt