FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG USERNAME=agent
ARG USER_UID=1001
ARG USER_GID=$USER_UID
ARG NODE_MAJOR=22
ARG PNPM_VERSION=11.18.0
ARG CLAUDE_CODE_VERSION=latest

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_TOOLS=/usr/local/share/dotnet-tools
ENV PATH="${PATH}:${DOTNET_TOOLS}"

# Claude Code refuses --dangerously-skip-permissions when running as root on Linux,
# so the agents need an unprivileged user to run as.
RUN groupadd --gid $USER_GID $USERNAME \
    && useradd --uid $USER_UID --gid $USER_GID --create-home --shell /bin/bash $USERNAME

RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
        ca-certificates \
        curl \
        git \
        gnupg \
        jq \
        less \
    && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL "https://deb.nodesource.com/setup_${NODE_MAJOR}.x" | bash - \
    && apt-get install --yes --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# Crypter.Web runs pnpm install and several vite build scripts in a PreBuild target,
# so a solution build fails without pnpm. Pinned to the version CI uses.
RUN npm install --global "pnpm@${PNPM_VERSION}" \
    && npm install --global "@anthropic-ai/claude-code@${CLAUDE_CODE_VERSION}" \
    && npm cache clean --force

RUN dotnet workload install wasm-tools

RUN dotnet tool install dotnet-ef --version '10.0.*' --tool-path "${DOTNET_TOOLS}"

COPY .devcontainer/clone-upstream.sh /usr/local/bin/crypter-clone-upstream
RUN chmod +x /usr/local/bin/crypter-clone-upstream

# The workspace and the agent's Claude Code state are both named volumes. Docker creates a
# mount point that the image does not already contain as root, so creating these here is
# what gives the volumes the right ownership.
RUN mkdir -p /work /home/$USERNAME/.claude \
    && chown $USER_UID:$USER_GID /work /home/$USERNAME/.claude

USER $USERNAME
WORKDIR /work
