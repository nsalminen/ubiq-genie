import { NetworkId } from 'ubiq';
import { ApplicationController } from '../../components/application';
import { TextToSpeechService } from '../../services/text_to_speech/service';
import { SpeechToTextService } from '../../services/speech_to_text/service';
import { CodeGenerationService } from '../../services/code_generation/service';
import { MediaReceiver } from '../../components/media_receiver';
import path from 'path';
import { RTCAudioData } from '@roamhq/wrtc/types/nonstandard';
import { fileURLToPath } from 'url';

export class CodeGenerationAgent extends ApplicationController {
    components: {
        mediaReceiver?: MediaReceiver;
        speech2text?: SpeechToTextService;
        codeGenerationService?: CodeGenerationService;
    } = {};
    isGenerating: boolean = false;
    targetPeer: string = '';
    constructor(configFile: string = 'config.json') {
        super(configFile);
    }

    start(): void {
        // STEP 1: Register services (and any other components) used by the application
        this.registerComponents();
        this.log(`Services registered: ${Object.keys(this.components).join(', ')}`);

        // STEP 2: Define the application pipeline
        this.definePipeline();
        this.log('Pipeline defined');

        // STEP 3: Join a room based on the configuration (optionally creates a server)
        this.joinRoom();
    }

    registerComponents() {
        // An MediaReceiver to receive audio data from peers
        this.components.mediaReceiver = new MediaReceiver(this.scene);

        // A SpeechToTextService to transcribe audio coming from peers
        this.components.speech2text = new SpeechToTextService(this.scene);

        // A CodeGenerationService to generate text based on text
        this.components.codeGenerationService = new CodeGenerationService(this.scene);

        this.isGenerating = false;

    }

    definePipeline() {
        // Step 1: When we receive audio data from a peer we send it to the transcription service and recording service
        this.components.mediaReceiver?.on('audio', (uuid: string, data: RTCAudioData) => {
            // Convert the Int16Array to a Buffer
            const sampleBuffer = Buffer.from(data.samples.buffer);

            // Send the audio data to the transcription service and the audio recording service
            this.components.speech2text?.sendToChildProcess(uuid, sampleBuffer);
        });

        // Step 2: When we receive a response from the transcription service, we send it to the text generation service
        this.components.speech2text?.on('data', (data: Buffer, identifier: string) => {
            // We obtain the peer object from the room client using the identifier
            const peer = this.roomClient.peers.get(identifier);
            const peerName = peer?.properties.get('ubiq.displayname');

            let response = data.toString();
            var threshold = 10; //for filtering useless responses
            if (response.length != 0 && response.length > threshold && this.isGenerating == false) {
                // Remove all newlines from the response
                response = response.replace(/(\r\n|\n|\r)/gm, '');
                console.log("Step 2");
                console.log(response);
                if (response.startsWith('>')) {
                    response = response.slice(1); // Slice off the leading '>' character
                    if (response.trim()) {
                        const message = (peerName + ' -> Agent:: ' + response).trim();
                        this.log(message);

                        this.components.codeGenerationService?.sendToChildProcess('default', message + '\n');
                    }
                }
            }
        });

        // Step 3: When we receive a response from the text generation service, we send it to the text to speech service
        this.components.codeGenerationService?.on('data', (data: Buffer, identifier: string) => {
            const response = data.toString();
            console.log('Received text generation response from child process ' + identifier + ': ' + response);
            console.log("Step 3");
            // Parse target peer from the response (Agent -> TargetPeer: Message)
            if (response.startsWith(">")) {
                console.log(" -> Code:: " + response);
                const cleaned_response = response.slice(1);
                
                this.scene.send(94, {
                        type: "CodeGenerated",
                        peer: identifier,
                        data: cleaned_response,
                    });
                this.isGenerating = false;
            }
        });

    }
}

if (fileURLToPath(import.meta.url) === path.resolve(process.argv[1])) {
    const configPath = './config.json';
    const __dirname = path.dirname(fileURLToPath(import.meta.url));
    const absConfigPath = path.resolve(__dirname, configPath);
    const app = new CodeGenerationAgent(absConfigPath);
    app.start();
}
