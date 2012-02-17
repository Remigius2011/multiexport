Run Exports of Test Repos

1. Install TortoiseGit, TortoiseHg and Tortoise Svn and make sure
   the respective command line tools are on the PATH by invoking
   the commands git, hg and svn from any command line
2. Extract vss.zip to this directory (creates a subdirectory vss etc.)
3. Run Vss2Git in batch mode with the following command:
   Vss2Git -b C:\Projects\InHouse\Vss2Git\test\VssTest-0-*.properties
4. Check log files in test/logs
5. Verify the content (history, labels etc.) of the exported repos
   with the respective tortoise
