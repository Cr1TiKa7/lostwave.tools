dotnet publish -c Release -r linux-x64 --self-contained false -o ./linux
scp -r ./linux root@194.163.144.213:/var/lostwavetools