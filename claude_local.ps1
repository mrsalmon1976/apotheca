# OMLX Models: gemma-4-E4B-it-MLX-4bit / Qwen2.5-Coder-14B-Instruct-MLX-4bit
$model = "gemma-4-E4B-it-MLX-4bit"
# Redirect Claude Code to your local Ollama server
# $env:ANTHROPIC_BASE_URL = "http://192.168.0.174:11434"
$env:ANTHROPIC_BASE_URL = "http://192.168.0.174:8000"
# Local servers do not require real authentication
# Set these to any non-empty string -- Ollama ignores the value
$env:ANTHROPIC_API_KEY = ""
$env:ANTHROPIC_AUTH_TOKEN = ""
# Map Claude Code's model tier requests to your local model name
# Claude Code internally requests sonnet/haiku/opus -- these variables
# translate those tier names to whatever model you have pulled locally
$env:ANTHROPIC_DEFAULT_SONNET_MODEL = $model
$env:ANTHROPIC_DEFAULT_HAIKU_MODEL = $model
$env:ANTHROPIC_DEFAULT_OPUS_MODEL = $model
$env:ANTHROPIC_MODEL = $model

$env:API_TIMEOUT_MS = "3000000"
$env:CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC = "1"
# Launch Claude Code -- it will now use Ollama instead of the Anthropic API
claude