import React, { useEffect, useState } from "react";
import { GetMatchHistory } from "./api/user";
import { useNavigate } from "react-router-dom";

function MatchHistory() {
    const navigate = useNavigate();
    const [historyData, setHistoryData] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [hoveredId, setHoveredId] = useState(null);

    useEffect(() => {
        const fetchMatchHistory = async () => {
            const token = localStorage.getItem("userToken");
            if (!token) {
                setError("You must be logged in to view match history.");
                setLoading(false);
                return;
            }

            try {
                const response = await GetMatchHistory(token);
                if (!response.ok) throw new Error("Failed to fetch match history");
                const data = await response.json();
                setHistoryData(data);
            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };

        fetchMatchHistory();
    }, []);

    if(loading) {
        return <div>Loading...</div>;
    }
    if(error) {
        return <div>{error}</div>;
    }

  
    return (
        <div fontSize="18px">
            <div>
                <h1>Replays</h1>
                
                <div style={{ marginTop: "38px" }}>
                    <table style={{ width: "100%", borderCollapse: "collapse" }}>
                        <thead>
                            <tr>
                                <th style={{ border: "1px solid #ddd", padding: "8px" }}>Game</th>
                                <th style={{ border: "1px solid #ddd", padding: "8px" }}>Date</th>
                                <th style={{ border: "1px solid #ddd", padding: "8px" }}>Result</th>
                            </tr>
                        </thead>
                        {historyData.length === 0 ? (
                              <tr>
                                 <td colSpan="3">No match history found</td>
                              </tr>
                           ) : (
                              historyData.map(item => (
                                 <tr 
                                   key={item.id} 
                                   onClick={() => navigate("/replay", { state: { matchData: item } })}
                                   onMouseEnter={() => setHoveredId(item.id)}
                                   onMouseLeave={() => setHoveredId(null)}
                                   style={{ 
                                      cursor: "pointer",
                                      backgroundColor: hoveredId === item.id ? "#cc7a00" : "transparent",
                                      transition: "background-color 0.2s ease"
                                   }}
                                 >
                                    <td style={{ border: "1px solid #ddd", padding: "8px" }}>{item.gameType}</td>
                                    <td style={{ border: "1px solid #ddd", padding: "8px" }}>{new Date(item.timestamp).toLocaleString()}</td>
                                    <td style={{ border: "1px solid #ddd", padding: "8px" }}>{item.matchStatus}</td>
                                 </tr>
                              ))
                           )}
                    </table>
                </div>
                
            </div>
            <div style={{ marginBottom: "20px" }}>
                <p 
                    onClick={() => navigate("/profile")}
                    style={{ 
                        padding: "8px 16px", 
                        fontSize: "14px", 
                        cursor: "pointer",
                        marginTop: "20px",
                        color: "white",
                        border: "none",
                       
                    }}
                >
                    Back to Profile
                </p>
            </div>
        </div>
    )

}

export default MatchHistory;