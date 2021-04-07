# GitHub Proxy

Proxy access to github.com for computers in restricted network environment.

## Prerequisites
* You hava your own domain where multiple subdomains can be added.
* Your server hava port 80 and 443 available and can be access from the Internet.

## Installation Steps

### Step 1: Add A or AAAA records to your domain with your IP address.

The following subdomain should be added. Replace `yourdomain.example.com` with your own domain.
```
yourdomain.example.com
blackhole.yourdomain.example.com
assets.yourdomain.example.com
avatars.yourdomain.example.com
raw.yourdomain.example.com
camo.yourdomain.example.com
codeload.yourdomain.example.com
releases.yourdomain.example.com
user-images.yourdomain.example.com
```
Your can use wildcard if your service provider allows it.

### Step 2: Download executables from Releases page.

Extract the package on your server and set the "execute" attribute.
```
chmod +x GitHubProxy
```

### Step 3: Modify appsettings.json to match your domain name.
You can also modify the `Urls` field to control which port the proxy will listen on. For example, change it to `http://localhost:9000` will listen on port `9000`.

### Step 4: Configure the application to always run on system startup

This is the systemd config file for example.
```
[Unit]
Description=GitHub Proxy

[Service]
Type=notify
WorkingDirectory=/opt/github_proxy
ExecStart=/opt/github_proxy/GitHubProxy

[Install]
WantedBy=multi-user.target
```

### Step 5: Install Caddy on your server to enable HTTPS

```
apt update
apt install caddy
```

### Step 6: Configure Caddy
Modify caddy file to forward requests for the domains you hava configured in Step 1.
```
http://yourdomain.example.com {
  redir https://{host}{uri}
}
https://yourdomain.example.com {
  reverse_proxy 127.0.0.1:9000
}

# 8 other domains omitted
```

### Step 7: Configure Caddy to always run on system startup
```
systemctl enable caddy
```

### Step 8: Restart services
```
systemctl daemon-reload
systemctl restart github-proxy
systemctl restart caddy
```
