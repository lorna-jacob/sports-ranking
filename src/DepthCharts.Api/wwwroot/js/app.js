const API_BASE = '/api/depthchart';
const TEAMS_API = '/api/teams';

async function loadTeams() {
    try {
        const response = await fetch(TEAMS_API);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const teams = await response.json();
        const teamSelect = document.getElementById('teamId');

        teamSelect.innerHTML = '<option value="">Select a team...</option>';

        teams.forEach(team => {
            const option = document.createElement('option');
            option.value = team.id;
            option.textContent = `${team.name} (${team.id})`;
            teamSelect.appendChild(option);
        });

        const tbOption = teamSelect.querySelector('option[value="TB"]');
        if (tbOption) {
            teamSelect.value = 'TB';
            loadDepthChart();
        }
    } catch (error) {
        console.error('Load teams error:', error);
        showMessage('Failed to load teams: ' + error.message, 'error');
    }
}

async function addPlayer() {
    const teamId = document.getElementById('teamId').value;
    const position = document.getElementById('addPosition').value;
    const playerNumber = parseInt(document.getElementById('addPlayerNumber').value);
    const playerName = document.getElementById('addPlayerName').value;
    const depth = document.getElementById('addDepth').value;

    if (!teamId || !position || !playerNumber || !playerName) {
        showMessage('Please fill in all required fields', 'error');
        return;
    }

    const payload = {
        position: position,
        player: { number: playerNumber, name: playerName, teamId: teamId },
        positionDepth: depth ? parseInt(depth) : null       
    };

    try {
        const response = await fetch(`${API_BASE}/${teamId}/players`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            showMessage(`Added ${playerName} successfully!`, 'success');
            clearAddForm();
            loadDepthChart();
        } else {
            const error = await response.json();
            showMessage(error.error || 'Failed to add player', 'error');
        }
    } catch (error) {
        showMessage('Error calling the api: ' + error.message, 'error');
    }
}

async function removePlayer() {
    const teamId = document.getElementById('teamId').value;
    const position = document.getElementById('removePosition').value;
    const playerNumber = parseInt(document.getElementById('removePlayerNumber').value);

    if (!teamId || !position || !playerNumber) {
        showMessage('Please fill in all fields', 'error');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/${teamId}/positions/${position}/players/${playerNumber}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            const removedPlayer = await response.json();
            showMessage(`Removed ${removedPlayer.name} successfully!`, 'success');
            clearRemoveForm();
            loadDepthChart();
        } else if (response.status === 404) {
            showMessage('Player not found at that position', 'error');
        } else {
            const error = await response.json();
            showMessage(error.error || 'Failed to remove player', 'error');
        }
    } catch (error) {
        showMessage('Error calling the api: ' + error.message, 'error');
    }
}

async function getBackups() {
    const teamId = document.getElementById('teamId').value;
    const position = document.getElementById('backupPosition').value;
    const playerNumber = parseInt(document.getElementById('backupPlayerNumber').value);

    if (!teamId || !position || !playerNumber) {
        showMessage('Please fill in all fields for backup lookup', 'error');
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/${teamId}/positions/${position}/players/${playerNumber}/backups`);

        if (response.ok) {
            const backups = await response.json();
            displayBackups(backups, position, playerNumber);
            clearBackupForm();
        } else if (response.status === 404) {
            showMessage('Player not found at that position', 'error');
        } else {
            const error = await response.json();
            showMessage(error.error || 'Failed to get backups', 'error');
        }
    } catch (error) {
        showMessage('Error calling the api: ' + error.message, 'error');
    }
}

async function loadDepthChart() {
    const teamId = document.getElementById('teamId').value;
    if (!teamId) {
        document.getElementById('depthChart').innerHTML = '<p>Please select a team to view depth chart.</p>';
        return;
    }

    try {
        const response = await fetch(`${API_BASE}/${teamId}/depthchart`);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const groupedChart = await response.json();
        displayDepthChart(groupedChart);
    } catch (error) {
        console.error('Load depth chart error:', error);
        showMessage('Failed to load depth chart: ' + error.message, 'error');
    }
}

function displayDepthChart(groupedChart) {
    const container = document.getElementById('depthChart');
    const teamId = document.getElementById('teamId').value;
    const teamSelect = document.getElementById('teamId');
    const selectedTeamText = teamSelect.options[teamSelect.selectedIndex]?.text || teamId;

    if (!groupedChart || Object.keys(groupedChart).length === 0) {
        container.innerHTML = `<h3>${selectedTeamText} Depth Chart</h3><p>No players in depth chart yet. Add some players to get started!</p>`;
        return;
    }

    let html = `<h3>${selectedTeamText} Depth Chart</h3>`;

    for (const [groupName, positionsArray] of Object.entries(groupedChart)) {
        html += `<h4>${groupName}</h4>`;

        let maxDepth = 0;
        if (Array.isArray(positionsArray)) {
            for (const positionData of positionsArray) {
                const players = positionData.players || [];
                maxDepth = Math.max(maxDepth, players.length);
            }
        }

        html += '<table class="depth-chart-table">';

        html += '<tr>';
        html += '<th class="position-header">Position</th>';
        for (let i = 1; i <= Math.max(maxDepth, 5); i++) {
            html += `<th class="player-header">Player ${i}</th>`;
        }
        html += '</tr>';

        if (Array.isArray(positionsArray)) {
            for (const positionData of positionsArray) {
                const position = positionData.position || 'Unknown Position';
                const players = positionData.players || [];

                console.log('Processing position:', position, 'with players:', players);

                html += '<tr>';
                html += `<td class="position-cell"><strong>${position}</strong></td>`;

                for (let i = 0; i < Math.max(maxDepth, 5); i++) {
                    if (i < players.length) {
                        const player = players[i];
                        html += `<td class="player-cell">#${player.number}<br>${player.name}</td>`;
                    } else {
                        html += '<td class="player-cell empty-cell">-</td>';
                    }
                }

                html += '</tr>';
            }
        }

        html += '</table>';
    }

    container.innerHTML = html;
}

function displayBackups(backups, position, playerNumber) {
    const container = document.getElementById('backupResults');

    if (!backups || backups.length === 0) {
        container.innerHTML = `<div class="backup-result">
            <h4>Backups for Player #${playerNumber} at ${position}:</h4>
            <p><em>No backup players found.</em></p>
        </div>`;
        return;
    }

    let html = `<div class="backup-result">
        <h4>Backups for Player #${playerNumber} at ${position}:</h4>
        <ul>`;

    backups.forEach(backup => {
        html += `<li>#${backup.number} - ${backup.name}</li>`;
    });

    html += `</ul></div>`;
    container.innerHTML = html;
}

function showMessage(text, type) {
    const messageDiv = document.getElementById('message');
    messageDiv.innerHTML = `<div class="${type}">${text}</div>`;
    setTimeout(() => messageDiv.innerHTML = '', 5000);
}

function clearAddForm() {
    document.getElementById('addPosition').value = '';
    document.getElementById('addPlayerNumber').value = '';
    document.getElementById('addPlayerName').value = '';
    document.getElementById('addDepth').value = '';
}

function clearRemoveForm() {
    document.getElementById('removePosition').value = '';
    document.getElementById('removePlayerNumber').value = '';
}

function clearBackupForm() {
    document.getElementById('backupPosition').value = '';
    document.getElementById('backupPlayerNumber').value = '';
}

document.addEventListener('DOMContentLoaded', function () {
    loadTeams();
});