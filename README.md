# RenovaLab VR

Simulador de conservação patrimonial criado pela equipe Dinolab no modelo 3D do Sebrae Lab. O visitante identifica uma rachadura, uma área com umidade e um trecho com tinta descascando. Cada reparo exige uma ferramenta virtual e restaura a aparência da parede.

## Abrir o projeto no Unity

1. No GitHub, use **Code → Download ZIP** ou clone o repositório. Extraia o ZIP antes de abrir o projeto.
2. No Unity Hub, adicione a pasta que contém `Assets`, `Packages` e `ProjectSettings`. Use o Unity **6000.0.83f1**.
3. No painel **Project**, abra `Assets/Scenes/sebrae.unity` e pressione **Play**. No computador, use **WASD** para andar, segure o **botão direito do mouse** para olhar, use **Tab** para trocar de ferramenta e o **botão esquerdo** para identificar ou aplicar. Ao concluir, **R** reinicia a visita.

## Testar no Meta Quest

Baixe `RenovaLabVR.apk` na seção [Releases](https://github.com/israelads31/projeto_sebraelab/releases) e instale no Quest preparado para receber APKs. O aplicativo roda no próprio óculos após a instalação. O teste no aparelho está reservado para a aula com o professor.

- Aponte o controle direito para o dano e aperte o **gatilho** para identificar.
- Use **A** para selecionar a ferramenta indicada e segure o **gatilho** na área até concluir o reparo.
- Use os **analógicos** para andar e girar. Depois dos três reparos, pressione **B** para reiniciar.

Para gerar outro APK, abra o projeto a partir de um caminho sem acentos e use **File → Build Profiles → Android** com os módulos de compilação Android do Unity instalados. A cena `sebrae` já está na lista de cenas de compilação.
