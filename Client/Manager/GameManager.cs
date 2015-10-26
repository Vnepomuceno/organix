using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("ProcessList")]
public class GameManager {
	
	public enum GameScreen { Intro, Instructions, Login, Home,
		ProcessCreation, ComposedCreation, AdHocCreation,
		ProcessVersionCreate, VersionComposedCreation, VersionAdHocCreation,
		ViewProcess, ViewComposed, ViewAdHoc, GameOver };
	
	public Player CurrentPlayer;
	public ClientState GameState;
	public GameScreen CurrentScreen;
	public Process CurrentProcess;
	public Primitive InspectorPrimitive;
	public Primitive InspectorSubPrimitive;
	public Lane CurrentLane;
	public ProcessVersion CurrentVersion;
	public NotificationSystem Notifications;
	
	public float GameLength = 3600;
	public float RemainingSeconds = 3600;
	public bool GameOn;
	public string ToElicitProcessName = "";
	
	public GameManager() {
		GameState = new ClientState();
		CurrentScreen = GameScreen.Intro;
		Notifications = new NotificationSystem();
	}
	
	public void LoadPlayerScore(string username) {
		if (!CurrentPlayer.Username.Equals(username))
			return;
		
		int scoreP = 0, scoreV = 0, NumDraftProcesses = 0, nrVersions = 0,
			numActivP = 0, numActivV = 0, numDupP = 0, numDupV = 0,
			totNumVotesP = 0, totNumVotesV = 0, numVotesP = 0,
			numVotesV = 0,numConvP = 0, numConvV = 0;
		
		foreach (Process process in GameState.LocalProcesses) {
			if (process.published && process.Author.Equals(username)) {
				scoreP += process.score;
				NumDraftProcesses++;
				numActivP += process.CalculateNumberActivities();
				numDupP += (process.markedDuplication) ? 1 : 0;
			}
			
			if (process.published) {
				foreach (Vote qualVote in process.QualityVotes) {
					totNumVotesP++;
					if (qualVote.GetVoter().Equals(Painter.Manager.CurrentPlayer.Username)) {
						numVotesP++;
						if (qualVote.GetVote() && process.posVotes > process.negVotes) {
							numConvP++;
							scoreP += 10;
						}
					}
				}
				
				foreach (Vote dupVote in process.DuplicationVotes) {
					totNumVotesP++;
					if (dupVote.GetVoter().Equals(Painter.Manager.CurrentPlayer.Username)) {
						numVotesP++;
						if (dupVote.GetVote() && process.posDuplicationVotes > process.negDuplicationVotes) {
							numConvP++;
							scoreP += 20;
						}
					}
				}
			}
			
			foreach (ProcessVersion version in process.Versions) {
				if (version.published && version.Author.Equals(username)) {
					scoreV += version.score;
					nrVersions++;
					numActivV += version.CalculateNumberActivities();
					numDupV += (version.markedDuplication) ? 1 : 0;
				}
				
				if (version.published) {
					foreach (Vote qualVote in version.QualityVotes) {
						totNumVotesV++;
						if (qualVote.GetVoter().Equals(Painter.Manager.CurrentPlayer.Username)) {
							numVotesV++;
							if (qualVote.GetVote() && version.posVotes > version.negVotes) {
								numConvV++;
								scoreV += 10;
							}
						}
					}
					
					foreach (Vote dupVote in version.DuplicationVotes) {
						totNumVotesV++;
						if (dupVote.GetVoter().Equals(Painter.Manager.CurrentPlayer.Username)) {
							numVotesV++;
							if (dupVote.GetVote() && process.posDuplicationVotes > process.negDuplicationVotes) {
								numConvV++;
								scoreV += 10;
							}
						}
					}
				}
			}
		}
		
		CurrentPlayer.Score = scoreP + scoreV;
		CurrentPlayer.AvgProcessScore = (NumDraftProcesses != 0) ? scoreP / NumDraftProcesses : 0;
		CurrentPlayer.AvgVersionScore = (nrVersions != 0) ? scoreV / nrVersions : 0;
		CurrentPlayer.AvgProcessActiv = (NumDraftProcesses != 0) ? numActivP / NumDraftProcesses : 0;
		CurrentPlayer.AvgVersionActiv = (nrVersions != 0) ? numActivV / nrVersions : 0;
		CurrentPlayer.NumDupProcesses = numDupP;
		CurrentPlayer.NumDupVersions = numDupV;
		CurrentPlayer.NumDraftProcesses = NumDraftProcesses;
		CurrentPlayer.NumDraftVersions = nrVersions;
		CurrentPlayer.NumVotesProcesses = numVotesP;
		CurrentPlayer.NumVotesVersions = numVotesV;
		CurrentPlayer.ConvRateProcesses = (numVotesP != 0) ? ((float)numConvP/totNumVotesP) : 0;
		CurrentPlayer.ConvRateVersions = (numVotesV != 0) ? ((float)numConvV/totNumVotesV) : 0;
	}
}
