# OneClickToProd
On stop app to publish a new version of a web site manage by SVN and host on a Linux server

## How it (suposed to) work

1. Connect with SSH on the remote hosting server
2. SVN update file on this server
3. Close SSH connection
4. Create a tag in the SVN server
5. Update configuration table in the database to set the version of SVN revision number

## Credits
This project is made using these 3th party tools:

* [SSH.Net](https://sshnet.codeplex.com/)
* [sharpsvn](https://sharpsvn.open.collab.net/servlets/ProjectProcess?documentContainer=c4__Samples)
