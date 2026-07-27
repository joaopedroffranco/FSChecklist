#requires -Version 5.1

param(
    [string]$ChecklistDirectory = (Join-Path $PSScriptRoot 'checklists')
)

Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Speech

[xml]$xaml = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="FSChecklist" Width="760" Height="610" MinWidth="680" MinHeight="540"
        WindowStartupLocation="CenterScreen" Background="#10141B" Foreground="#F4F7FB">
  <Window.Resources>
    <Style TargetType="Button">
      <Setter Property="Background" Value="#2374E1"/>
      <Setter Property="Foreground" Value="White"/>
      <Setter Property="BorderThickness" Value="0"/>
      <Setter Property="Padding" Value="16,10"/>
      <Setter Property="Margin" Value="4"/>
      <Setter Property="FontWeight" Value="SemiBold"/>
    </Style>
    <Style TargetType="ComboBox">
      <Setter Property="Margin" Value="4"/>
      <Setter Property="Padding" Value="8"/>
      <Setter Property="MinHeight" Value="38"/>
    </Style>
  </Window.Resources>
  <Grid Margin="24">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <StackPanel Grid.Row="0">
      <TextBlock Text="FSChecklist" FontSize="30" FontWeight="Bold"/>
      <TextBlock Text="Callouts locais, na ordem exata do seu JSON" Foreground="#9EABBC" Margin="0,4,0,16"/>
    </StackPanel>

    <Grid Grid.Row="1">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <StackPanel Grid.Column="0">
        <TextBlock Text="Aeronave" Foreground="#B7C2D0" Margin="4,0"/>
        <ComboBox Name="AircraftBox"/>
      </StackPanel>
      <StackPanel Grid.Column="1">
        <TextBlock Text="Checklist" Foreground="#B7C2D0" Margin="4,0"/>
        <ComboBox Name="ChecklistBox"/>
      </StackPanel>
      <Button Grid.Column="2" Name="StartButton" Content="INICIAR" VerticalAlignment="Bottom"/>
    </Grid>

    <Border Grid.Row="2" Background="#18202B" CornerRadius="10" Padding="24" Margin="4,18">
      <Grid>
        <Grid.RowDefinitions>
          <RowDefinition Height="Auto"/>
          <RowDefinition Height="*"/>
          <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <DockPanel>
          <TextBlock Name="ProgressText" Text="Nenhuma checklist iniciada" Foreground="#91A0B3"/>
          <TextBlock Name="StateBadge" Text="PRONTO" Foreground="#5ED39B" FontWeight="Bold" DockPanel.Dock="Right"/>
        </DockPanel>
        <StackPanel Grid.Row="1" VerticalAlignment="Center">
          <TextBlock Name="ChallengeText" Text="Selecione uma aeronave e uma checklist" FontSize="32"
                     FontWeight="Bold" TextAlignment="Center" TextWrapping="Wrap"/>
          <TextBlock Name="ExpectedText" Text="" FontSize="17" Foreground="#91A0B3"
                     TextAlignment="Center" TextWrapping="Wrap" Margin="0,16,0,0"/>
        </StackPanel>
        <TextBlock Grid.Row="2" Name="HeardText" Text="Aguardando inicio" Foreground="#A9B7C8"
                   TextAlignment="Center" TextWrapping="Wrap"/>
      </Grid>
    </Border>

    <Grid Grid.Row="3">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="2*"/>
        <ColumnDefinition Width="*"/>
      </Grid.ColumnDefinitions>
      <Button Grid.Column="0" Name="BackButton" Content="&lt; VOLTAR" Background="#344256"/>
      <Button Grid.Column="1" Name="PttButton" Content="SEGURE PARA FALAR - F9" FontSize="17" Padding="18"/>
      <Button Grid.Column="2" Name="RepeatButton" Content="REPETIR" Background="#344256"/>
    </Grid>

    <TextBlock Grid.Row="4" Name="StatusText" Text="Carregando checklists..." Foreground="#8F9BAD"
               Margin="4,14,4,0" TextAlignment="Center"/>
  </Grid>
</Window>
'@

$reader = New-Object System.Xml.XmlNodeReader $xaml
$window = [Windows.Markup.XamlReader]::Load($reader)

function Control([string]$name) { $window.FindName($name) }
$aircraftBox = Control 'AircraftBox'
$checklistBox = Control 'ChecklistBox'
$startButton = Control 'StartButton'
$backButton = Control 'BackButton'
$repeatButton = Control 'RepeatButton'
$pttButton = Control 'PttButton'
$challengeText = Control 'ChallengeText'
$expectedText = Control 'ExpectedText'
$progressText = Control 'ProgressText'
$stateBadge = Control 'StateBadge'
$heardText = Control 'HeardText'
$statusText = Control 'StatusText'

$script:documents = @()
$script:activeChecklist = $null
$script:itemIndex = -1
$script:isListening = $false
$script:synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$script:recognizer = $null

function Normalize-Speech([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return '' }
    $formD = $text.ToLowerInvariant().Normalize([Text.NormalizationForm]::FormD)
    $builder = New-Object Text.StringBuilder
    foreach ($character in $formD.ToCharArray()) {
        if ([Globalization.CharUnicodeInfo]::GetUnicodeCategory($character) -ne
            [Globalization.UnicodeCategory]::NonSpacingMark) {
            [void]$builder.Append($character)
        }
    }
    return (($builder.ToString() -replace '[^a-z0-9]+', ' ').Trim())
}

function Speak([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return }
    $script:synth.SpeakAsyncCancelAll()
    [void]$script:synth.SpeakAsync($text)
}

function Get-CurrentItem {
    if (-not $script:activeChecklist -or $script:itemIndex -lt 0) { return $null }
    return @($script:activeChecklist.items)[$script:itemIndex]
}

function Get-ItemCallout($item) {
    if ($item -is [string]) { return [string]$item }
    return [string]$item.callout
}

function Get-ItemResponses($item) {
    if ($item -is [string]) { return @() }
    return @($item.responses)
}

function Set-CurrentItem {
    if (-not $script:activeChecklist) { return }
    $items = @($script:activeChecklist.items)
    if ($script:itemIndex -ge $items.Count) {
        $challengeText.Text = "$($script:activeChecklist.name) completa"
        $expectedText.Text = 'Todos os itens foram confirmados.'
        $progressText.Text = "$($items.Count) de $($items.Count)"
        $stateBadge.Text = 'COMPLETA'
        $stateBadge.Foreground = '#5ED39B'
        $heardText.Text = 'Checklist completa'
        Speak $script:activeChecklist.completedCallout
        return
    }

    $item = $items[$script:itemIndex]
    $challengeText.Text = Get-ItemCallout $item
    $responses = @(Get-ItemResponses $item)
    if ($script:activeChecklist.acceptAnyAnswer -or $responses.Count -eq 0) {
        $expectedText.Text = 'Confirmacao por voz: qualquer resposta reconhecida'
    } else {
        $expectedText.Text = 'Resposta esperada: ' + ($responses -join ' / ')
    }
    $progressText.Text = "Item $($script:itemIndex + 1) de $($items.Count)"
    $stateBadge.Text = 'PENDENTE'
    $stateBadge.Foreground = '#FFCA58'
    $heardText.Text = 'Segure o botao ou F9 para responder'
    Speak (Get-ItemCallout $item)
}

function Start-SelectedChecklist {
    $aircraft = [string]$aircraftBox.SelectedItem
    $name = [string]$checklistBox.SelectedItem
    $document = $script:documents | Where-Object { $_.aircraft -eq $aircraft } | Select-Object -First 1
    $script:activeChecklist = @($document.checklists | Where-Object { $_.name -eq $name })[0]
    if (-not $script:activeChecklist) { return }
    $script:itemIndex = 0
    $heardText.Text = 'Checklist iniciada'
    Set-CurrentItem
}

function Test-PilotResponse([string]$spokenText) {
    if (-not $script:activeChecklist -or $script:itemIndex -lt 0) { return }
    $items = @($script:activeChecklist.items)
    if ($script:itemIndex -ge $items.Count) { return }

    $heardText.Text = "Ouvido: $spokenText"
    $heard = Normalize-Speech $spokenText
    $accepted = @(Get-ItemResponses $items[$script:itemIndex]) |
        ForEach-Object { Normalize-Speech ([string]$_) }
    $matched = $script:activeChecklist.acceptAnyAnswer -and $heard.Length -gt 0
    if (-not $matched) {
        foreach ($answer in $accepted) {
            if ($heard -eq $answer -or $heard -match "(^| )$([regex]::Escape($answer))( |$)") {
                $matched = $true
                break
            }
        }
    }

    if ($matched) {
        Stop-Listening
        $stateBadge.Text = 'CONFIRMADO'
        $stateBadge.Foreground = '#5ED39B'
        $script:itemIndex++
        $timer = New-Object Windows.Threading.DispatcherTimer
        $timer.Interval = [TimeSpan]::FromMilliseconds(550)
        $timer.Add_Tick({
            param($sender, $eventArgs)
            $sender.Stop()
            Set-CurrentItem
        })
        $timer.Start()
    } else {
        $stateBadge.Text = 'NAO CONFIRMADO'
        $stateBadge.Foreground = '#FF6B70'
        $statusText.Text = 'A resposta nao coincide com o JSON. O item permanece pendente.'
        Speak 'Nao confirmado'
    }
}

function Start-Listening {
    if ($script:isListening -or -not $script:recognizer) { return }
    $script:synth.SpeakAsyncCancelAll()
    $script:isListening = $true
    $pttButton.Background = '#D84B55'
    $pttButton.Content = 'OUVINDO... SOLTE PARA ENVIAR'
    $stateBadge.Text = 'OUVINDO'
    try { $script:recognizer.RecognizeAsync([System.Speech.Recognition.RecognizeMode]::Single) }
    catch { $statusText.Text = "Microfone indisponivel: $($_.Exception.Message)" }
}

function Stop-Listening {
    if (-not $script:isListening -or -not $script:recognizer) { return }
    $script:isListening = $false
    $pttButton.Background = '#2374E1'
    $pttButton.Content = 'SEGURE PARA FALAR - F9'
    try { $script:recognizer.RecognizeAsyncStop() } catch {}
}

function Load-Checklists {
    if (-not (Test-Path $ChecklistDirectory)) {
        New-Item -ItemType Directory -Path $ChecklistDirectory | Out-Null
    }
    foreach ($file in Get-ChildItem $ChecklistDirectory -Filter '*.json' -File) {
        try {
            $document = Get-Content $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
            if (-not $document.aircraft -or -not $document.checklists) {
                throw 'Campos obrigatorios: aircraft e checklists.'
            }
            foreach ($checklist in @($document.checklists)) {
                $checklist | Add-Member -NotePropertyName acceptAnyAnswer `
                    -NotePropertyValue ([bool]$document.rules.acceptAnyAnswer) -Force
                if (-not $checklist.completedCallout) {
                    $checklist | Add-Member -NotePropertyName completedCallout `
                        -NotePropertyValue "$($checklist.name) checklist complete" -Force
                }
            }
            $script:documents += $document
        } catch {
            $statusText.Text = "JSON invalido em $($file.Name): $($_.Exception.Message)"
        }
    }

    @($script:documents | ForEach-Object aircraft | Sort-Object -Unique) |
        ForEach-Object { [void]$aircraftBox.Items.Add($_) }
    if ($aircraftBox.Items.Count -gt 0) {
        $aircraftBox.SelectedIndex = 0
        $statusText.Text = "$($script:documents.Count) arquivo(s) carregado(s). Tudo funciona localmente."
    } else {
        $statusText.Text = "Nenhuma checklist encontrada em $ChecklistDirectory"
    }
}

$aircraftBox.Add_SelectionChanged({
    $checklistBox.Items.Clear()
    $selected = [string]$aircraftBox.SelectedItem
    $document = $script:documents | Where-Object aircraft -eq $selected | Select-Object -First 1
    @($document.checklists) | ForEach-Object { [void]$checklistBox.Items.Add($_.name) }
    if ($checklistBox.Items.Count -gt 0) { $checklistBox.SelectedIndex = 0 }
})
$startButton.Add_Click({ Start-SelectedChecklist })
$repeatButton.Add_Click({
    if ($script:activeChecklist -and $script:itemIndex -ge 0 -and
        $script:itemIndex -lt @($script:activeChecklist.items).Count) {
        Speak (Get-ItemCallout (Get-CurrentItem))
    }
})
$backButton.Add_Click({
    if ($script:activeChecklist -and $script:itemIndex -gt 0) {
        $script:itemIndex--
        Set-CurrentItem
    }
})
$pttButton.Add_PreviewMouseLeftButtonDown({ Start-Listening })
$pttButton.Add_PreviewMouseLeftButtonUp({ Stop-Listening })
$pttButton.Add_LostMouseCapture({ if ($script:isListening) { Stop-Listening } })
$window.Add_PreviewKeyDown({
    param($sender, $eventArgs)
    if ($eventArgs.Key -eq [Windows.Input.Key]::F9 -and -not $eventArgs.IsRepeat) {
        Start-Listening
        $eventArgs.Handled = $true
    }
})
$window.Add_PreviewKeyUp({
    param($sender, $eventArgs)
    if ($eventArgs.Key -eq [Windows.Input.Key]::F9) {
        Stop-Listening
        $eventArgs.Handled = $true
    }
})
$window.Add_Closed({
    if ($script:recognizer) {
        try { $script:recognizer.RecognizeAsyncCancel() } catch {}
        $script:recognizer.Dispose()
    }
    $script:synth.Dispose()
})

try {
    $culture = [Globalization.CultureInfo]::GetCultureInfo('pt-BR')
    $script:recognizer = New-Object System.Speech.Recognition.SpeechRecognitionEngine $culture
} catch {
    try {
        $script:recognizer = New-Object System.Speech.Recognition.SpeechRecognitionEngine
        $statusText.Text = 'pt-BR nao instalado; usando o reconhecedor padrao do Windows.'
    } catch {
        $statusText.Text = 'Reconhecimento de voz nao esta instalado no Windows.'
    }
}

if ($script:recognizer) {
    try {
        $script:recognizer.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
        $script:recognizer.SetInputToDefaultAudioDevice()
        $script:recognizer.Add_SpeechRecognized({
            param($sender, $eventArgs)
            if ($eventArgs.Result.Confidence -ge 0.45) {
                Test-PilotResponse $eventArgs.Result.Text
            } else {
                $heardText.Text = "Fala incerta: $($eventArgs.Result.Text) - tente novamente"
            }
        })
        $script:recognizer.Add_RecognizeCompleted({
            $script:isListening = $false
            $pttButton.Background = '#2374E1'
            $pttButton.Content = 'SEGURE PARA FALAR - F9'
        })
    } catch {
        $statusText.Text = "Microfone indisponivel: $($_.Exception.Message)"
        $script:recognizer.Dispose()
        $script:recognizer = $null
        $pttButton.IsEnabled = $false
    }
}

Load-Checklists
[void]$window.ShowDialog()
