# PsicViewer

Aplicativo de monitoramento de humor e suporte psicológico, conectando **Paciente** e **Psicólogo** — desenvolvido como Trabalho de Conclusão de Curso.

## 🧱 Tecnologias

- **.NET 8 MAUI** — aplicativo mobile (Android)
- **ASP.NET Core Web API** — backend
- **MySQL** (via Entity Framework Core / Pomelo) — persistência de dados
- **SignalR** — chat em tempo real
- **Clean Architecture** — Core / Application / Infrastructure / Api

A API está hospedada no **Azure** (App Service + MySQL Flexible Server), então não é necessário rodar nenhum servidor localmente para testar o app.

## ▶️ Como executar

1. Clona o repositório
2. Abre a solução **`MauiApp1.sln`** no Visual Studio 2022 (com a carga de trabalho **.NET Multi-platform App UI development** instalada)
3. No **Solution Explorer** (painel lateral com a lista de projetos), clica com o **botão direito** em cima do projeto **`MauiApp1`** → **Set as Startup Project** (definir como projeto de inicialização). Ele deve ficar em **negrito** na lista — é assim que se confirma que está certo. A solução tem 5 projetos ao todo (`MauiApp1`, `PsicViewer.Api`, `PsicViewer.Core`, `PsicViewer.Infrastructure`, `PsicViewer.Application`), mas só o `MauiApp1` precisa rodar — os outros são bibliotecas que ele usa por baixo dos panos, não têm um "programa" próprio pra executar.
4. Escolhe uma das duas opções abaixo pra rodar o app, e clica no botão verde ▶️ (ou aperta **F5**). O app já se conecta à API hospedada no Azure, sem nenhuma configuração adicional.

### Opção A — Celular Android físico

1. No celular, vai em **Ajustes → Sobre o telefone** e toca **7 vezes seguidas** em "Número da versão" (ou "Build number") — isso libera o **Modo Desenvolvedor**
2. Volta em **Ajustes**, agora deve aparecer uma opção nova chamada **Opções do desenvolvedor** — entra nela
3. Ativa a **Depuração USB** (Ligar/Desligar)
4. Conecta o celular no PC por cabo USB. Pode aparecer um aviso no celular perguntando se confia no computador — toca em **Permitir**
5. No Visual Studio, no seletor de dispositivo (barra de ferramentas, ao lado do botão ▶️), o celular deve aparecer listado pelo nome do modelo — seleciona ele
6. Clica em ▶️ — o Visual Studio instala e abre o app direto no celular

### Opção B — Emulador Android

1. Se ainda não tiver um emulador criado, abre o **Android Device Manager** (acessível pelo seletor de dispositivo, na barra de ferramentas do Visual Studio) e cria um novo.

   **Configuração recomendada** (testada e funcionando):

   | Campo | Valor |
   |---|---|
   | Dispositivo | Pixel 5 |
   | Imagem do sistema | Android 12.0 – API 31 (Google APIs) |
   | Processador | x86_64 |
   | Memória | 1 GB |
   | Resolução | 1080 x 2340, 440 dpi |

2. No seletor de dispositivo do Visual Studio, escolhe o emulador criado
3. Clica em ▶️ — o emulador abre, e o Visual Studio instala e abre o app nele

**Se aparecer o aviso "O Hyper-V não está configurado"** ao iniciar o emulador: isso acontece porque a aceleração de hardware do Windows ainda não está ativada na máquina.
- **Solução rápida:** clica em **"Executar Mesmo Assim"** — o emulador abre e funciona, só que mais lento (sem aceleração).
- **Solução definitiva:** aperta `Win + R` → digita `optionalfeatures` → Enter. No Windows **Pro/Enterprise/Education**, marca **Hyper-V**; no Windows **Home** (que não tem Hyper-V), marca **"Plataforma de Hipervisor do Windows"** no lugar. Reinicia o PC depois de marcar. Se o aviso continuar aparecendo mesmo depois de reiniciar, confere se a virtualização (Intel VT-x / AMD-V) está ativada na BIOS/UEFI da placa-mãe.

## 👤 Contas de teste

Pra explorar o app sem precisar cadastrar do zero:

| Perfil | E-mail | Senha |
|---|---|---|
| Psicóloga | `ana@teste.com` | `123456` |
| Paciente | `pedro@teste.com` | `123456` |
| Paciente | `julia@teste.com` | `123456` |

Ana já está vinculada a Pedro e Julia, com um questionário de humor ativo — os dois pacientes têm respostas registradas nos últimos 14 dias, então já dá pra ver o **Histórico** e os **Relatórios de Humor** funcionando com dados reais sem precisar gerar nada manualmente.

## 📱 Como usar

### Como Paciente

1. Faz login (ou cria uma conta em "Sou paciente")
2. Em **Chat**, toca em "Buscar Psicólogo" pra encontrar e solicitar vínculo com um profissional (ou aceita um convite, se o psicólogo te procurar primeiro)
3. Em **Questionários**, responde as perguntas do dia — pode ser em escala (rostinhos), texto livre ou múltipla escolha, com espaço pra observações em texto ou áudio
4. Acompanha suas próprias respostas em **Histórico**
5. Conversa com seu psicólogo pelo **Chat** — texto, áudio, foto ou documento

### Como Psicólogo

1. Faz login (ou cria uma conta em "Sou psicólogo")
2. Em **Pacientes**, busca e convida pacientes, ou aceita solicitações recebidas
3. Em **Questionários**, cria questionários personalizados (perguntas de escala, texto ou múltipla escolha, cada uma com seu horário de notificação) e vincula aos pacientes
4. Acompanha o **Histórico** de respostas de cada paciente, e gera **Relatórios de Humor** com gráficos comparando diferentes perguntas ao longo do tempo
5. Ao ver uma resposta nova (via notificação no sino), pode responder com um **Feedback** (texto ou áudio), que aparece pro paciente direto no chat, citando a pergunta e resposta original

## 🏗️ Estrutura do projeto

```
PsicViewer.Core            → Entidades e interfaces de domínio
PsicViewer.Application      → Casos de uso
PsicViewer.Infrastructure    → EF Core, repositórios (MySQL)
PsicViewer.Api               → API (SignalR + endpoints REST)
MauiApp1                    → Aplicativo mobile (MVVM)
```
