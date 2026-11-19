# Employee Registry API

API do registro de funcionários. ASP.NET Core no .NET 10, controllers, Entity Framework Core e SQLite.

O front-end fica em outro repositório. No Kubernetes, um Ingress envia `/api` para este serviço e o restante para a interface. O navegador continua chamando `/api` no mesmo endereço.

## Rodar localmente

Instale o [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run
```

A API escuta em http://localhost:3000. O primeiro start cria `employee-registry.sqlite`, aplica as migrations e grava duas contas e três funcionários.

| Login | Senha | Papel |
| --- | --- | --- |
| admin | Admin@123 | Admin |
| viewer | Viewer@123 | Viewer |

As senhas e a chave JWT de desenvolvimento estão em `Configuration/appsettings.Development.json`. Fora de Development, defina `Jwt__Key` com pelo menos 32 caracteres. Os nomes estão em `.env.example`.

OpenAPI, só em Development: http://localhost:3000/openapi/v1.json

## Imagem

```bash
docker build -t ghcr.io/leohernandezqae/employee-registry-api:latest .
```

A imagem escuta na porta 8080.

## Kubernetes

Há um único réplica. SQLite é um arquivo e um escritor; mais de um pod corrompe o banco.

```bash
kubectl apply -f deploy/pvc.yaml
kubectl apply -f deploy/secret.example.yaml
kubectl apply -f deploy/deployment.yaml
kubectl apply -f deploy/service.yaml
```

Antes de aplicar o secret, troque os valores de `deploy/secret.example.yaml`. O Ingress que junta a interface e esta API está no repositório da interface.

O GitHub Actions publica a imagem em `ghcr.io/leohernandezqae/employee-registry-api` a cada push na `main`.
