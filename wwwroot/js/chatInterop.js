window.chatInterop = {
 
  playAudio: (audioFile) => {
      const audio = new Audio(audioFile);
      audio.play().catch(e => console.error("Audio playback failed:", e));
  },

  pauseAudio: () => {
      const audios = document.getElementsByTagName('audio');
      for (let audio of audios) {
          audio.pause();
      }
  },

  playAudioWithCallback: (audioFile, dotnetRef) => {
      const audio = new Audio(audioFile);
      audio.play().catch(e => {
          console.error("Audio playback failed:", e);
          dotnetRef.invokeMethodAsync('OnAudioEnded');
      });
      audio.onended = () => {
          dotnetRef.invokeMethodAsync('OnAudioEnded');
          dotnetRef.dispose();
      };
  },

  recordingHandles: {},

// Check if recording is supported
checkRecordingSupport: async function() {
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    return false;
  }
  
  try {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    stream.getTracks().forEach(track => track.stop());
    return true;
  } catch (err) {
    console.error("Recording not supported:", err);
    return false;
  }
},

startRecording: async function(sessionId) {
  console.log("startRecording called with sessionId:", sessionId);
  
  try {
    // Initialize recordingHandles if it doesn't exist
    if (!this.recordingHandles) {
      this.recordingHandles = {};
    }
    
    // Check if already recording
    if (this.recordingHandles[sessionId]) {
      console.log("Already recording this session");
      return false;
    }
    
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      console.error("Media devices not supported");
      return false;
    }
    const stream = await navigator.mediaDevices.getUserMedia({ 
      audio: {
          sampleRate: 16000,  // Suggest preferred rate (browser may ignore)
          channelCount: 1,    // Force mono
          noiseSuppression: true,
          echoCancellation: true
      } 
  });
    console.log("Got media stream");
    
    const options = { mimeType: 'audio/webm' };
    const mediaRecorder = new MediaRecorder(stream, options);
    const audioChunks = [];
    
    mediaRecorder.ondataavailable = function(event) {
      if (event.data.size > 0) {
        audioChunks.push(event.data);
        console.log("Data chunk added, size:", event.data.size);
      }
    };
    
    mediaRecorder.start(250);
    console.log("MediaRecorder started");
    
    // Store recording handle
    this.recordingHandles[sessionId] = {
      mediaRecorder: mediaRecorder,
      audioChunks: audioChunks,
      stream: stream
    };
    
    console.log("Recording started for session:", sessionId);
    console.log("Current recordingHandles:", Object.keys(this.recordingHandles));
    
    return true;
  } catch (error) {
    console.error("Error starting recording:", error);
    return false;
  }
},

stopRecording: async function (sessionId) {
  const recorderObj = window.chatInterop.recordingHandles?.[sessionId];
  if (!recorderObj) {
    console.error('No recorder found for sessionId:', sessionId);
    return null;
  }

  return new Promise((resolve, reject) => {
    if (!recorderObj.mediaRecorder) {
      console.error('MediaRecorder not found');
      resolve(null);
      return;
    }

    if (recorderObj.mediaRecorder.state !== 'inactive') {
      recorderObj.mediaRecorder.onstop = async () => {
        try {
          if (!recorderObj.audioChunks || recorderObj.audioChunks.length === 0) {
            console.error('No audio chunks');
            resolve(null);
            return;
          }

          const audioBlob = new Blob(recorderObj.audioChunks, { type: 'audio/webm' });
          const arrayBuffer = await audioBlob.arrayBuffer();
          const uint8Array = new Uint8Array(arrayBuffer);

          // Clean up
          if (recorderObj.stream) {
            recorderObj.stream.getTracks().forEach(track => track.stop());
          }
          delete window.chatInterop.recordingHandles[sessionId];

          resolve(uint8Array);
        } catch (err) {
          console.error('Error handling audio data:', err);
          resolve(null);
        }
      };

      try {
        recorderObj.mediaRecorder.stop();
      } catch (err) {
        console.error('Error stopping mediaRecorder:', err);
        resolve(null);
      }
    } else {
      console.log('MediaRecorder already inactive, creating blob immediately');
      (async () => {
        try {
          const audioBlob = new Blob(recorderObj.audioChunks, { type: 'audio/webm' });
          const arrayBuffer = await audioBlob.arrayBuffer();
          const uint8Array = new Uint8Array(arrayBuffer);

          if (recorderObj.stream) {
            recorderObj.stream.getTracks().forEach(track => track.stop());
          }
          delete window.chatInterop.recordingHandles[sessionId];

          resolve(uint8Array);
        } catch (err) {
          console.error('Error creating blob from inactive recorder:', err);
          resolve(null);
        }
      })();
    }
  });
}, // File download
  downloadFile: function (filename, content, contentType) {
      // Create a blob with the content
      const blob = new Blob([content], { type: contentType || 'application/octet-stream' });
      const url = URL.createObjectURL(blob);

      // Create a temporary anchor element
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();

      // Cleanup
      setTimeout(() => {
          document.body.removeChild(a);
          URL.revokeObjectURL(url);
      }, 100);
  },

  scrollToBottom: function (element) {
      element.scrollTop = element.scrollHeight;
  }
};
