SPT LAUNCHER - instruções
==========================

REQUISITOS
- Windows 10/11
- .NET 8 SDK instalado (https://dotnet.microsoft.com/download/dotnet/8.0)
  Escolhe o "SDK", não só o "Runtime".

COMPILAR PELA CMD
1. Abre a CMD (ou PowerShell) na pasta deste projeto (onde está o LauncherSPT.csproj).
2. Corre:

     dotnet build -c Release

   O executável fica em:
     bin\Release\net8.0-windows\LauncherSPT.exe

   Para gerar um único .exe portátil (mais fácil de distribuir), usa em vez disso:

     dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

   O ficheiro final fica em:
     bin\Release\net8.0-windows\win-x64\publish\LauncherSPT.exe

3. Corre o LauncherSPT.exe.

COMO FUNCIONA (SEM SPT.LAUNCHER)
Este launcher já NÃO abre o SPT.Launcher.exe. Em vez disso:
1. Lê os teus perfis diretamente da pasta "user\profiles\*.json" da tua
   instalação SPT e mostra-os num menu para escolheres.
2. Ao clicar em JOGAR, arranca o SPT.Server.exe escondido (sem consola).
3. Assim que o servidor está pronto, arranca o EscapeFromTarkov.exe
   DIRETAMENTE, passando o ID do perfil escolhido como token de sessão
   e o endereço do servidor como argumento de arranque - exatamente
   como o SPT.Launcher faria, mas sem precisares de abrir uma segunda
   janela.

Esta abordagem é baseada no projeto open-source QuickLauncher, de
minihazel (https://github.com/minihazel/QuickLauncher), que usa a mesma
técnica (arranque direto do EscapeFromTarkov.exe com -token e -config).

PRIMEIRA UTILIZAÇÃO
1. Abre o launcher e clica no ícone de engrenagem (⚙) no canto superior direito.
2. Em "SPT.Server.exe" clica em Procurar e seleciona o ficheiro do servidor
   (normalmente dentro da pasta de instalação do SPT, ex: C:\SPT\SPT.Server.exe).
   O EscapeFromTarkov.exe é detetado automaticamente na mesma pasta - só
   precisas de o indicar manualmente se estiver noutro sítio.
3. (Opcional) Define uma imagem de fundo e os textos do título.
4. Clica em Guardar.
5. De volta à janela principal, escolhe o teu perfil na lista "PERFIL"
   (usa o botão ⟳ para atualizar a lista se acabaste de criar um perfil).

IMPORTANTE - PRECISO DE JÁ TER UM PERFIL CRIADO
Já não precisas de correr o SPT.Launcher.exe para criar perfis - podes
criá-los e apagá-los diretamente neste launcher, na aba "PERFIS" da
janela principal (ver secção seguinte).

CRIAR E APAGAR PERFIS (aba "PERFIS" na janela principal)
A janela principal tem duas abas: "JOGAR" (onde escolhes o perfil e
entras no jogo) e "PERFIS" (onde crias e apagas perfis). Esta última
fala diretamente com a API do teu servidor SPT (as mesmas rotas que o
SPT.Launcher.exe usa: /launcher/profile/register e
/launcher/profile/remove), por isso funciona sem precisares de abrir
mais nenhuma outra aplicação. Mais abas podem ser adicionadas aqui no
futuro, tal como estas duas.

- CRIAR: na aba PERFIS, escreve um nome de utilizador, escolhe a
  edição (Standard, EOD, Unheard, etc. - lidas diretamente do teu
  servidor) e clica em "CRIAR PERFIL". Se o servidor ainda não estiver
  a correr, este launcher arranca-o automaticamente (escondido) antes
  de criar o perfil.
- APAGAR: clica em "APAGAR" ao lado do perfil na lista. É pedida
  confirmação antes de remover, porque a ação não pode ser desfeita.
- Depois de criar ou apagar um perfil, a lista da aba JOGAR atualiza-se
  automaticamente.

Nota: um perfil recém-criado ainda não tem nickname/nível, porque o
nome da personagem só é definido dentro do próprio jogo, no ecrã de
criação de personagem, na primeira vez que entrares com esse perfil.
Até lá aparece na lista com o nome de utilizador que escolheste.

USAR
- Escolhe o perfil na lista.
- Clica em JOGAR: o launcher arranca o servidor escondido, espera que
  esteja pronto, e entra diretamente no jogo com esse perfil.
- Clica em PARAR SERVIDOR para terminar o servidor manualmente.
- Se fechares esta janela, o servidor que foi iniciado por ela é
  terminado automaticamente (o jogo, se já tiver arrancado, continua a
  correr).

NOTA SOBRE O VISUAL
O launcher usa uma paleta escura com verde-oliva e laranja e tipografia
tática (Consolas), inspirada no estilo do EFT, mas não reproduz o
logótipo nem artwork oficial (direitos de autor) — usa apenas texto e
elementos originais, que podes editar nas Definições.
