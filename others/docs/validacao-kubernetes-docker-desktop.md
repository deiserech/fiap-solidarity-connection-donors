# Validacao em Kubernetes com Docker Desktop

## Objetivo

Subir os dois microsservicos no Kubernetes do Docker Desktop e comprovar o fluxo assincrono de doacao.

## Pre-requisitos

1. Docker Desktop com Kubernetes habilitado.
2. `kubectl` configurado para o contexto `docker-desktop`.
3. Repositorios locais:
   - `c:/Repos/HACKATON/fiap-solidarity-connection`
   - `c:/Repos/HACKATON/fiap-solidarity-connection-donations`
4. SQL Server acessivel.
5. Azure Service Bus acessivel.

## Passo 1 - Confirmar contexto Kubernetes

```bash
kubectl config current-context
```

Resultado esperado: `docker-desktop`.

## Passo 2 - Build local das imagens

No repo Core:

```bash
docker build -f src/SolidarityConnection.Api/Dockerfile -t solidarityconnectionacr.azurecr.io/solidarity-connection:latest .
```

No repo Donations:

```bash
docker build -f src/SolidarityConnection.Donations.Api/Dockerfile -t solidarityconnectionacr.azurecr.io/solidarity-connection-donations:latest .
```

Observacao:

- Os deployments usam `imagePullPolicy: IfNotPresent` para aproveitar imagem local no Docker Desktop.

## Passo 3 - Preencher secrets

Editar os placeholders antes de aplicar:

1. `k8s/users-secret.yaml` (Core)
2. `k8s/shared-secret.yaml` (Core)
3. `../fiap-solidarity-connection-donations/k8s/donations-secret.yaml`
4. `../fiap-solidarity-connection-donations/k8s/shared-secret.yaml`

Campos minimos:

- `ConnectionStrings__DefaultConnection`
- `ServiceBus__ConnectionString`
- `JwtSettings__SecretKey`
- `JwtSettings__Issuer`
- `JwtSettings__Audience`
- `NEW_RELIC_LICENSE_KEY` (opcional para validacao local)

## Passo 4 - Aplicar manifests (ordem recomendada)

No repo Core:

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/users-secret.yaml
kubectl apply -f k8s/shared-secret.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/hpa.yaml
```

No repo Donations:

```bash
kubectl apply -f k8s/donations-secret.yaml
kubectl apply -f k8s/shared-secret.yaml
kubectl apply -f k8s/donations-configmap.yaml
kubectl apply -f k8s/donations-service.yaml
kubectl apply -f k8s/donations-deployment.yaml
kubectl apply -f k8s/donations-hpa.yaml
```

## Passo 5 - Validar rollout

```bash
kubectl get pods -n solidarity-connection
kubectl get deploy -n solidarity-connection
kubectl get svc -n solidarity-connection
```

Todos os pods devem ficar `Running`.

## Passo 6 - Port-forward para validar APIs

Terminal 1 (Core):

```bash
kubectl port-forward -n solidarity-connection svc/solidarity-connection 8080:8083
```

Terminal 2 (Donations):

```bash
kubectl port-forward -n solidarity-connection svc/solidarity-connection-donations 8081:8082
```

Health checks:

```bash
curl http://localhost:8080/health
curl http://localhost:8081/health
```

## Passo 7 - Validacao funcional fim a fim

1. Login na API Core.
2. Criar/usar campanha ativa.
3. Enviar `POST /api/donations` no Core.
4. Verificar atualizacao em `GET /api/campaigns/public`.

Critero de aceite:

1. Mensageria funcionando via Service Bus.
2. Donations processa e publica evento.
3. Core atualiza `total_raised_amount` no proprio banco.

## Comandos de suporte

Logs Core:

```bash
kubectl logs -n solidarity-connection deploy/solidarity-connection --tail=200
```

Logs Donations:

```bash
kubectl logs -n solidarity-connection deploy/solidarity-connection-donations --tail=200
```

Remocao total:

```bash
kubectl delete namespace solidarity-connection
```
