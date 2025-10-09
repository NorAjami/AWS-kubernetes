# EKS Setup Guide - Steg för steg med alla detaljer

Jag kommer guida dig genom hela processen! Vi börjar från början och jag förklarar allt längs vägen.

## 🎯 Vad vi ska bygga

En Kubernetes-cluster i AWS där vi kör en enkel webbserver (nginx) som är tillgänglig från internet via en load balancer.

---

## 📋 Förberedelser

### Steg 0.1: Verifiera AWS CLI

Öppna **Terminal** (på Mac) eller **Command Prompt/PowerShell** (på Windows).

Kör detta kommando:

```bash
aws --version
```

**Förväntat resultat:** Du ska se något som `aws-cli/2.x.x`

**Om du får ett felmeddelande:**

- Du behöver installera AWS CLI först
- Besök: https://aws.amazon.com/cli/
- Ladda ner och installera för ditt operativsystem
- Starta om terminalen efter installation

### Steg 0.2: Konfigurera AWS CLI

Vi behöver koppla AWS CLI till ditt AWS-konto.

Kör:

```bash
aws configure
```

Du kommer få fyra frågor:

1. **AWS Access Key ID:** (Får du från AWS Console → IAM → Security credentials)
2. **AWS Secret Access Key:** (Samma ställe som ovan)
3. **Default region name:** Skriv `eu-west-1` (det är Irland)
4. **Default output format:** Tryck bara Enter (använder standard)

💡 **Tips:** Om du inte har Access Keys:

- Logga in på AWS Console
- Klicka ditt användarnamn uppe till höger → Security credentials
- Scrolla ner till "Access keys" → Create access key
- Välj "Command Line Interface (CLI)"
- Kopiera både Key ID och Secret Key (de visas bara en gång!)

### Steg 0.3: Installera kubectl

**kubectl** (uttalas "kube-control") är verktyget vi använder för att prata med Kubernetes.

**På Mac:**

```bash
brew install kubectl
```

**På Windows:** Ladda ner från: https://kubernetes.io/docs/tasks/tools/install-kubectl-windows/

**Verifiera installation:**

```bash
kubectl version --client
```

Du ska se versionsnummer (t.ex. `Client Version: v1.28.x`)

---

## 🚀 Steg 1: Skapa EKS Cluster

### Steg 1.1: Öppna AWS Console

1. Gå till https://console.aws.amazon.com
2. Logga in
3. I sökfältet uppe, skriv **EKS**
4. Klicka på **Elastic Kubernetes Service**

### Steg 1.2: Starta Cluster-skapande

1. Klicka på den orange knappen **"Create cluster"**
2. Du ser nu flera alternativ - välj **"Quick configuration"** (ska vara förvald)

### Steg 1.3: Grundinställningar

Fyll i:

- **Cluster name:** `my-cluster` (eller välj eget namn)
- **Kubernetes version:** Välj den senaste versionen i listan (t.ex. 1.31 eller nyare)

💡 **Vad är Kubernetes version?** Det är vilken version av Kubernetes-mjukvaran som ska köras. Välj alltid senaste för nya projekt.

### Steg 1.4: IAM Roller (behörigheter)

**🔐 Vad är IAM roller?** Det är "passerkort" som ger AWS-tjänster tillåtelse att göra saker. EKS behöver två roller:

- En för själva cluster (hjärnan)
- En för worker nodes (maskinerna som kör dina applikationer)

#### Cluster IAM Role:

1. Klicka **"Create recommended role"** under "Cluster IAM role"
2. En ny flik öppnas → Klicka **"Create role"** (längst ner)
3. Stäng den fliken
4. Tillbaka i EKS-fliken: Klicka **refresh-ikonen** 🔄 bredvid dropdown-menyn
5. Välj **AmazonEKSAutoClusterRole** i listan

#### Node IAM Role:

1. Klicka **"Create recommended role"** under "Node IAM role"
2. En ny flik öppnas → Klicka **"Create role"**
3. Stäng den fliken
4. Tillbaka i EKS-fliken: Klicka **refresh-ikonen** 🔄
5. Välj **AmazonEKSAutoNodeRole** i listan

### Steg 1.5: Skapa VPC (nätverk)

**🌐 Vad är VPC?** Det är ditt eget privata nätverk i AWS. Tänk dig det som att bygga ett hus med olika rum - vissa rum är synliga från gatan (publika), andra är privata.

1. Klicka **"Create VPC"** under VPC-sektionen
2. En **ny flik** öppnas med VPC-wizard

#### I VPC-wizarden:

**Steg A - Grundinställningar:**

- **Resources to create:** `VPC and more` (ska vara förvalt)
- **Name tag auto-generation:** Ditt cluster-namn används automatiskt

**Steg B - Subnets (undernät):**

- **Number of Availability Zones:** Välj **2**
    - 💡 _Availability Zones = olika datacenter. Vi vill ha 2 för redundans (om ett kraschar fungerar det andra)_
- **Number of public subnets:** **2**
    - 💡 _Publika subnets = nåbara från internet, här placeras load balancer_
- **Number of private subnets:** **2**
    - 💡 _Privata subnets = ej nåbara direkt från internet, här körs dina applikationer (säkrare)_

**Steg C - NAT Gateway:**

- **NAT gateways:** Välj **1 per AZ**
    - 💡 _NAT Gateway = låter privata servrar surfa ut på internet (för uppdateringar etc.) men ingen kan surfa IN till dem_
    - ⚠️ **Kostnader:** ~$35/månad per NAT Gateway = $70/månad totalt

**Steg D - VPC Endpoints:**

- **VPC endpoints:** Markera **S3 Gateway**
    - 💡 _Detta är gratis och minskar kostnaderna för NAT Gateway genom att ge direkt access till S3_

**Steg E - VIKTIGA TAGS (scrolla ner!):**

1. Klicka **"Add new tag"**
2. Fyll i:
    - **Key:** `kubernetes.io/cluster/my-cluster` (ändra om du valde annat cluster-namn)
    - **Value:** `shared`

💡 **Varför denna tag?** Den säger till Kubernetes "dessa subnets tillhör mitt cluster". Den tillämpas på ALLA 4 subnets (både publika och privata).

3. Klicka **"Create VPC"** (orange knapp längst ner)
4. Vänta ~2 minuter tills du ser "Successfully created VPC"
5. **Stäng denna flik** och gå tillbaka till EKS-fliken

#### Tillbaka i EKS cluster creation:

1. Klicka **refresh-ikonen** 🔄 bredvid VPC-dropdown
2. Välj din nyskapade VPC (namnet börjar med ditt cluster-namn)
3. **Subnets:** EKS väljer automatiskt de 2 **privata** subnets (det är korrekt!)
    - 💡 _Dina pods (applikationer) ska köras i privata subnets för säkerhet_

### Steg 1.6: Skapa cluster!

1. Klicka **"Create cluster"** (orange knapp längst ner)
2. Du kommer till cluster-översikten
3. **Vänta 10-15 minuter** - Status ändras från "Creating" → "Active"

💡 **Perfekt tillfälle för en kaffepaus!** ☕

---

## ⚠️ KRITISKT: Steg 1.7 - Tagga Publika Subnets

**Varför är detta kritiskt?** Kubernetes behöver veta VILKA subnets som är publika för att kunna skapa internet-facing load balancers där. Utan denna tag kommer din load balancer att fastna i "pending" för evigt.

### Identifiera dina publika subnets:

1. Öppna en ny flik → Gå till **VPC Console**
2. I menyn till vänster, klicka **"Subnets"**
3. Hitta dina subnets:
    - Använd filter/sök efter ditt VPC-namn
    - Du ska se **4 subnets totalt**
    - **Publika subnets** känner du igen på:
        - "Auto-assign public IPv4 address" = **Yes**
        - Namnet innehåller ofta "public" eller "Public"
    - Du ska ha **2 publika subnets** (en per AZ)

### Tagga första publika subnet:

1. **Klicka i checkboxen** för första publika subnet
2. Klicka **"Actions"** → **"Manage tags"**
3. Klicka **"Add new tag"**
4. Fyll i:
    - **Key:** `kubernetes.io/role/elb`
    - **Value:** `1`
5. Klicka **"Save changes"**

### Tagga andra publika subnet:

Upprepa steg 1-5 för den andra publika subneten.

### Verifiera:

Båda dina **publika subnets** ska nu ha dessa tags:

- ✅ `kubernetes.io/cluster/my-cluster` = `shared` (från VPC-wizarden)
- ✅ `kubernetes.io/role/elb` = `1` (du just lade till)

**Dina privata subnets ska ENDAST ha:**

- ✅ `kubernetes.io/cluster/my-cluster` = `shared`
- ❌ INTE `kubernetes.io/role/elb` taggen!

---

## 💻 Steg 2: Anslut till Cluster

Nu ska vi koppla vårt kubectl-verktyg till cluster.

### Öppna Terminal/Command Prompt

Kör detta kommando:

```bash
aws eks update-kubeconfig --region eu-west-1 --name my-cluster
```

**Om du valde ett annat cluster-namn:** Byt ut `my-cluster` mot ditt namn.

**Förväntat resultat:**

```
Added new context arn:aws:eks:eu-west-1:xxxx:cluster/my-cluster to /Users/yourname/.kube/config
```

### Testa anslutningen:

```bash
kubectl get nodes
```

**Förväntat resultat:**

```
No resources found
```

💡 **Detta är KORREKT!** EKS Auto Mode skapar nodes först när du deployar pods. Smarta, eller hur?

---

## 🐳 Steg 3: Deploya Nginx

**Vad är nginx?** En populär webbserver. Vi använder den som test för att se att allt fungerar.

Kör detta kommando:

```bash
kubectl run my-nginx --image=nginx
```

**Förväntat resultat:**

```
pod/my-nginx created
```

**Vad händer nu bakom kulisserna:**

1. Kubernetes ber EKS: "Jag behöver en maskin!"
2. EKS Auto Mode: "OK, startar en EC2-instans..."
3. Efter 1-2 minuter: Nginx-containern startar

### Kolla status:

```bash
kubectl get pods
```

Du kommer se status ändras:

1. `Pending` (väntar på node)
2. `ContainerCreating` (node skapad, laddar nginx-image)
3. `Running` (allt klart! ✅)

Kör kommandot igen var 30:e sekund tills du ser `Running`.

Eller använd watch-läge:

```bash
kubectl get pods --watch
```

(Tryck `Ctrl+C` för att avsluta)

---

## 🌍 Steg 4: Exponera med Load Balancer

Nu ska vi göra nginx nåbar från internet!

### Steg 4.1: Skapa Service

```bash
kubectl expose pod my-nginx --type=LoadBalancer --port=80
```

**Förväntat resultat:**

```
service/my-nginx exposed
```

**Vad är en Service?** Det är Kubernetes sätt att exponera applikationer. `LoadBalancer`-typ skapar automatiskt en AWS Load Balancer.

### Steg 4.2: Lägg till internet-facing annotation

```bash
kubectl annotate service my-nginx service.beta.kubernetes.io/aws-load-balancer-scheme=internet-facing
```

**Varför behövs detta?** Som standard skapar AWS en _intern_ load balancer (endast tillgänglig inifrån VPC). Denna annotation säger: "Nej, jag vill ha en som är nåbar från internet!"

### Steg 4.3: Vänta på Load Balancer

Kör:

```bash
kubectl get service my-nginx --watch
```

Du kommer se:

```
NAME       TYPE           CLUSTER-IP      EXTERNAL-IP   PORT(S)        AGE
my-nginx   LoadBalancer   10.100.x.x      <pending>     80:xxxxx/TCP   10s
```

**Vänta 2-3 minuter...** EXTERNAL-IP ändras från `<pending>` till en lång DNS-adress:

```
my-nginx   LoadBalancer   10.100.x.x   k8s-default-mynginx-abc123.eu-west-1.elb.amazonaws.com   80:xxxxx/TCP   3m
```

Tryck `Ctrl+C` när du ser DNS-namnet.

---

## 🎉 Steg 5: Testa din Nginx!

### Hämta URL:en

**Alternativ 1 - Via kubectl:**

```bash
kubectl get service my-nginx
```

Kopiera värdet under `EXTERNAL-IP`.

**Alternativ 2 - Via AWS Console:**

1. Gå till **EC2 Console**
2. I menyn till vänster, klicka **"Load Balancers"**
3. Hitta load balancern (namnet börjar med `k8s-default-mynginx`)
4. Kopiera **DNS name**

### Öppna i webbläsare:

Öppna:

```
http://EXTERNAL-IP-FRÅN-OVAN
```

**Du ska se: Nginx välkomstsida!** 🎉

```
Welcome to nginx!
If you see this page, the nginx web server is successfully installed and working...
```

---

## 🧹 Cleanup - Rensa upp (viktigt för att undvika kostnader!)

### Steg 1: Ta bort Kubernetes-resurser

```bash
kubectl delete service my-nginx
kubectl delete pod my-nginx
```

**Vänta 5-10 minuter** - EKS Auto Mode tar automatiskt bort noden när inga pods körs längre.

### Steg 2: Ta bort EKS Cluster

1. Gå till **EKS Console**
2. Välj ditt cluster
3. Klicka **"Delete"**
4. Skriv cluster-namnet för att bekräfta
5. Vänta ~5 minuter

### Steg 3: Ta bort VPC

1. Gå till **VPC Console**
2. I menyn till vänster, klicka **"Your VPCs"**
3. Hitta ditt VPC (namnet innehåller ditt cluster-namn)
4. Klicka i checkboxen → **Actions** → **Delete VPC**
5. Bekräfta

💡 Detta tar bort **allt**: VPC, subnets, NAT Gateways, Internet Gateway, Route Tables, etc.


### steg 4: Släpp/release oanvända Elastic IPs

**För varje Elastic IP:**

Kolla kolumnen **"Associated instance ID"** eller **"AssociationId"**:

### Om den är TOM/None = Oanvänd ⚠️

**Denna kostar pengar ($0.005/timme = ~$3.60/månad) och kan släppas/release!**

**Släpp den:**

#### Via Console:

1. ✅ Välj Elastic IP
2. **Actions** → **Release Elastic IP addresses**
3. **Release**

#### Via CLI:

bash

```bash
aws ec2 release-address --allocation-id eipalloc-XXXXXXXXX
```

_(Ersätt med Allocation ID från listan)_

---

## 🔧 Felsökning

### Problem: EXTERNAL-IP fastnar på `<pending>`

**Kolla efter fel:**

```bash
kubectl describe service my-nginx
```

Scrolla ner till "Events"-sektionen. Vanliga felmeddelanden:

#### "Failed build model due to unable to resolve at least one subnet"

**Orsak:** Subnet-tags är fel. **Lösning:** Gå tillbaka till Steg 1.7 och verifiera taggarna igen.

#### "Evaluated 0 subnets"

**Orsak:** Du glömde internet-facing annotationen. **Lösning:**

```bash
kubectl annotate service my-nginx service.beta.kubernetes.io/aws-load-balancer-scheme=internet-facing
```

Om det inte hjälper, återskapa servicen:

```bash
kubectl delete service my-nginx
kubectl expose pod my-nginx --type=LoadBalancer --port=80
kubectl annotate service my-nginx service.beta.kubernetes.io/aws-load-balancer-scheme=internet-facing
```

### Problem: Pod fastnar på `Pending`

**Kolla status:**

```bash
kubectl describe pod my-nginx
```

**Orsak:** Noden skapas fortfarande (EKS Auto Mode tar 1-2 minuter). **Lösning:** Vänta och kolla igen.

---

## 📚 Sammanfattning & Lärdomar

### Vad du byggde:

1. ☁️ Ett Kubernetes-cluster i AWS (EKS)
2. 🌐 Ett VPC med 4 subnets (2 publika, 2 privata)
3. 🐳 En nginx-pod i privat subnet
4. ⚖️ En Classic Load Balancer i publika subnets
5. 🌍 Internet → Load Balancer → Pod dataflöde

### Viktiga koncept:

- **EKS Auto Mode:** Skapar och tar bort nodes automatiskt (du betalar bara när pods körs!)
- **Publika subnets:** Behöver `kubernetes.io/role/elb = 1` tag
- **Privata subnets:** Ska INTE ha elb-taggen
- **kubectl:** Ditt huvudverktyg för att prata med Kubernetes

### Tidsåtgång:

~20-25 minuter (varav 10-15 min väntetid för cluster)

### Kostnader (per månad om du lämnar allt igång):

- Control plane: ~$73
- Nodes: Varierar (Auto Mode = endast när pods körs)
- NAT Gateway: ~$70 (2 stycken à ~$35)
- Load Balancer: ~$20
- **Total: ~$163/månad**

⚠️ **Därför:** Ta bort allt när du är klar med labbet!

---

## 🎓 Nästa steg för lärande

Om du vill lära dig mer:

1. **Prova fler kubectl-kommandon:**
    
    ```bash
    kubectl get all                    # Visa alla resurser
    kubectl logs my-nginx             # Se loggar från pod
    kubectl exec -it my-nginx -- bash # Gå in i podden
    ```
    
2. **Deploya en riktig app:** Istället för nginx, prova att deploya din egen Docker-container
    
3. **Lär dig YAML:** Kubernetes-konfiguration skrivs i YAML-filer. Nästa steg är att lära sig skapa dessa istället för att köra `kubectl run`-kommandon
    
4. **Ingress Controller:** Ett mer avancerat sätt att hantera inkommande trafik än LoadBalancer
    

---

**Redo att börja?** Säg till när du är klar med varje steg så går vi vidare tillsammans! 🚀