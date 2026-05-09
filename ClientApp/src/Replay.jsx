import React, { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { GetUser } from "./api/user";

function Replay() {
    const navigate = useNavigate();
    const location = useLocation();
    const [matchData, setMatchData] = useState(null);
    const [currentStep, setCurrentStep] = useState(0);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(true);
    const [gameState, setGameState] = useState(null);
    const [currentUserId, setCurrentUserId] = useState(null);
    const [userAuthorized, setUserAuthorized] = useState(false);

    const getTimeStamp = (timestamp) => {
        const date = new Date(timestamp);
        return date.toLocaleString();
    };

    // Verify user and fetch match data
    useEffect(() => {
        const fetchUserAndMatchData = async () => {
            try {
                const token = localStorage.getItem("userToken");
                if (!token) throw new Error("You must be logged in to view replays.");

                // Get current user ID
                const userResponse = await GetUser(token);
                if (!userResponse.ok) throw new Error("Failed to verify user.");
                const userData = await userResponse.json();
                setCurrentUserId(userData.id);

                // Get match data from navigation state
                const data = location.state?.matchData;
                if (!data) throw new Error("No match data provided.");

                // Verify user is one of the players
                if (userData.id !== data.playerOneId && userData.id !== data.playerTwoId) {
                    throw new Error("You are not authorized to view this replay.");
                }

                setUserAuthorized(true);
                setMatchData(data);
                setGameState(computeGameState(data, 0));
            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };
        fetchUserAndMatchData();
    }, [location.state]);

    const computeGameState = (data, step) => {
        if (!data) return {};
        const { gameType, moves, playerOneId, playerTwoId, timestamp, matchStatus } = data;
        let state = getInitialState(gameType);
        const formattedTime = getTimeStamp(timestamp);

        if (!Array.isArray(moves)) return { ...state, formattedTime, matchStatus };

        for (let i = 0; i < step && i < moves.length; i++) {
            const move = moves[i];
            try {
                const moveData = JSON.parse(move.movesJson);
                const playerMark = move.userId === playerOneId ? 1 : 2;
                applyMove(state, moveData, playerMark, gameType);
            } catch (e) {
                console.error("Invalid move JSON:", move.movesJson);
            }
        }
        return { ...state, formattedTime, matchStatus };
    };

    const getInitialState = (gameType) => {
        switch (gameType) {
            case "TicTacToe":
                return { board: Array(3).fill().map(() => Array(3).fill(0)) };
            case "ConnectFour":
                return { board: Array(6).fill().map(() => Array(7).fill(0)) };
            case "RockPaperScissors":
                return { choices: [null, null] };
            default:
                return {};
        }
    };

    const applyMove = (state, moveData, playerMark, gameType) => {
        switch (gameType) {
            case "TicTacToe":
                if (state.board && moveData.X !== undefined && moveData.Y !== undefined) {
                    state.board[moveData.X][moveData.Y] = playerMark;
                }
                break;
            case "ConnectFour":
                if (state.board && moveData.Column !== undefined) {
                    for (let r = 5; r >= 0; r--) {
                        if (state.board[r][moveData.Column] === 0) {
                            state.board[r][moveData.Column] = playerMark;
                            break;
                        }
                    }
                }
                break;
            case "RockPaperScissors":
                const idx = playerMark - 1;
                // Choice values from backend: 0=Rock, 1=Paper, 2=Scissors
                if (state.choices && moveData.Choice !== undefined) {
                    state.choices[idx] = moveData.Choice;
                }
                break;
        }
    };

    useEffect(() => {
        if (matchData) {
            setGameState(computeGameState(matchData, currentStep));
        }
    }, [currentStep, matchData]);

    const handleNext = () => {
        if (matchData && currentStep < matchData.moves.length) {
            setCurrentStep(currentStep + 1);
        }
    };

    const handlePrevious = () => {
        if (currentStep > 0) {
            setCurrentStep(currentStep - 1);
        }
    };

    const renderGameBoard = () => {
        if (!matchData || !gameState) return <p>No game state available.</p>;

        const { gameType } = matchData;
        switch (gameType) {
            case "TicTacToe":
                return (
                    <div style={{ display: "grid", gridTemplateColumns: "repeat(3, 80px)", gap: "5px", alignContent: "center", justifyContent: "center", justifyItems: "center", alignItems: "center" }}>
                        {gameState.board && gameState.board.map((row, i) =>
                            row.map((cell, j) => (
                                <div key={`${i}-${j}`} style={{
                                    width: "80px", height: "80px", border: "2px solid black",
                                    display: "flex", alignItems: "center", justifyContent: "center", 
                                    fontSize: "32px", fontWeight: "bold", backgroundColor: "#182846"
                                }}>
                                    {cell === 1 ? "X" : cell === 2 ? "O" : ""}
                                </div>
                            ))
                        )}
                    </div>
                );
            case "ConnectFour":
                return (
                    <div style={{ display: "grid", gridTemplateColumns: "repeat(7, 60px)", gap: "5px", alignContent: "center", justifyContent: "center", justifyItems: "center", alignItems: "center" }}>
                        {gameState.board && gameState.board.map((row, i) =>
                            row.map((cell, j) => (
                                <div key={`${i}-${j}`} style={{
                                    width: "60px", height: "60px", border: "2px solid black", borderRadius: "50%",
                                    backgroundColor: cell === 1 ? "#ff4444" : cell === 2 ? "#ffff44" : "#ffffff"
                                }}></div>
                            ))
                        )}
                    </div>
                );
            case "RockPaperScissors":
                const choices = ["Rock", "Paper", "Scissors"];
                return (
                    <div style={{ margin: "20px 0", fontSize: "20px" }}>
                        <p><strong>{matchData.playerOneUsername}:</strong> {gameState.choices && gameState.choices[0] !== null ? choices[gameState.choices[0]] : "Waiting..."}</p>
                        <p><strong>{matchData.playerTwoUsername}:</strong> {gameState.choices && gameState.choices[1] !== null ? choices[gameState.choices[1]] : "Waiting..."}</p>
                    </div>
                );
            default:
                return <p>Unsupported game type.</p>;
        }
    };

    if (loading) return <div style={{ padding: "20px", fontSize: "18px" }}>Loading replay...</div>;
    if (error) return <div style={{ padding: "20px", fontSize: "18px", color: "red" }}>{error}</div>;
    if (!matchData) return <div style={{ padding: "20px", fontSize: "18px" }}>No match data available for replay.</div>;
    if (!userAuthorized) return <div style={{ padding: "20px", fontSize: "18px", color: "red" }}>You are not authorized to view this replay.</div>;

    const currentMove = currentStep > 0 ? matchData.moves[currentStep - 1] : null;
    const isLastStep = currentStep === matchData.moves.length;
    const totalMoves = matchData.moves ? matchData.moves.length : 0;

    let resultText = null;
    if (isLastStep) {
        if (matchData.winnerId) {
            resultText = matchData.winnerId === matchData.playerOneId 
                ? `${matchData.playerOneUsername} wins!` 
                : `${matchData.playerTwoUsername} wins!`;
        } else {
            resultText = "Draw!";
        }
    }

    return (
        <div style={{}}>
            

            <h2 style={{ marginBottom: "10px" }}>
                {matchData.gameType}
            </h2>
            <p style={{ marginBottom: "20px", fontSize: "16px", color: "#666" }}>
                {matchData.playerOneUsername} vs {matchData.playerTwoUsername}
            </p>
            <p style={{ marginBottom: "20px", fontSize: "14px", color: "#999" }}>
                {gameState?.formattedTime} | Status: {gameState?.matchStatus}
            </p>

            {renderGameBoard()}

            {currentMove && (
                <div style={{ marginTop: "20px", padding: "10px" }}>
                    <p style={{ margin: "0", fontSize: "16px" }}>
                        <strong>Move {currentStep}:</strong> {currentMove.username} played at {new Date(currentMove.playedAt).toLocaleString()}
                    </p>
                </div>
            )}

            {resultText && isLastStep && (
                <div style={{ marginTop: "20px", padding: "15px", backgroundColor: "#e8f5e9", border: "2px solid #4caf50", borderRadius: "4px" }}>
                    <h3 style={{ color: "#2e7d32", margin: "0" }}>{resultText}</h3>
                </div>
            )}

            <div style={{ marginTop: "30px", display: "flex", gap: "10px", alignItems: "center", justifyContent: "center" }}>
                <button 
                    onClick={handlePrevious} 
                    disabled={currentStep === 0}
                    style={{
                        padding: "10px 20px",
                        fontSize: "16px",
                        cursor: currentStep === 0 ? "not-allowed" : "pointer",
                        backgroundColor: currentStep === 0 ? "#cccccc" : "#ff9d00ff",
                        color: "white",
                        border: "none",
                        borderRadius: "4px",
                        opacity: currentStep === 0 ? 0.5 : 1
                    }}
                >
                    Previous
                </button>
                <span style={{ fontSize: "16px", fontWeight: "bold", minWidth: "50px", textAlign: "center" }}>
                    {currentStep}/{totalMoves}
                </span>
                <button 
                    onClick={handleNext} 
                    disabled={isLastStep}
                    style={{
                        padding: "10px 20px",
                        fontSize: "16px",
                        cursor: isLastStep ? "not-allowed" : "pointer",
                        backgroundColor: isLastStep ? "#cccccc" : "#44b17bff",
                        color: "white",
                        border: "none",
                        borderRadius: "4px",
                        opacity: isLastStep ? 0.5 : 1
                    }}
                >
                    Next
                </button>
            </div>

            <div style={{ marginBottom: "20px" }}>
                <p 
                    onClick={() => navigate("/matchHistory")}
                    style={{ 
                        padding: "8px 16px", 
                        fontSize: "14px", 
                        cursor: "pointer",
                        marginTop: "20px",
                        color: "white",
                        border: "none",
                       
                    }}
                >
                    Back to Match History
                </p>
            </div>
        </div>
    );
}

export default Replay;