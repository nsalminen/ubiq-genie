import { NetworkId } from 'ubiq';
import { ApplicationController } from '../../components/application';
import { SpeechToTextService } from '../../services/speech_to_text/service';
import { CodeGenerationService } from '../../services/code_generation/service';
import { UnderstandingService } from '../../services/understanding/service';
import { MediaReceiver } from '../../components/media_receiver';
import { MessageReader } from '../../components/message_reader';
import path from 'path';
import { RTCAudioData } from '@roamhq/wrtc/types/nonstandard';
import { fileURLToPath } from 'url';

export class CodeGenerationUnderstandingAgent extends ApplicationController {
    components: {
        mediaReceiver?: MediaReceiver;
        speech2text?: SpeechToTextService;
        codeGenerationService?: CodeGenerationService;
        selectionReceiver?: MessageReader;
        functionalityCoding?: MessageReader;
        understanding?: UnderstandingService;
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
        
        this.components.selectionReceiver = new MessageReader(this.scene, 94);
        this.components.functionalityCoding = new MessageReader(this.scene, 99);
        this.components.understanding = new UnderstandingService(this.scene);

        this.isGenerating = false;

    }

    definePipeline() {
        //this service receive the image and send to LLM 
        this.components.selectionReceiver.on('data', (data: any) => {
            const selectionData = JSON.parse(data.message.toString());
            const peerUUID = selectionData.peer;
            const selection = selectionData.selection;
            const triggerHeld = selectionData.triggerHeld; // True when trigger is held
            console.log("arrived image");
            this.components.understanding?.sendToChildProcess('default', selectionData.image + '\n'); //@@from here how to deal with image to the service FIRST
        });
        
        //this service retrieve information about object and functionalities
        this.components.understanding?.on('data', (data: Buffer, identifier: string) => {
            const response = data.toString();
            console.log('Received text generation response from child process ' + identifier + ': ' + response);
            console.log("---- Understanding");
            // Parse target peer from the response (Agent -> TargetPeer: Message)
            if (response.startsWith(">")) {
                //console.log(" -> Functions:: " + response);
                const cleaned_response = response.slice(1);
                
                this.scene.send(new NetworkId(94), {
                    type: "Detection",
                    peer: identifier,
                    data: cleaned_response,
                });
            }
        });
    
        // Step 1: When we receive audio data from a peer we send it to the transcription service and recording service
        this.components.mediaReceiver?.on('audio', (uuid: string, data: RTCAudioData) => {
            /*
            // Convert the Int16Array to a Buffer
            const sampleBuffer = Buffer.from(data.samples.buffer);

            // Send the audio data to the transcription service and the audio recording service
            if (this.roomClient.peers.get(uuid) !== undefined) {
                this.components.speech2text?.sendToChildProcess(uuid, sampleBuffer);
            }*/
        });

        // Step 2: When we receive a response from the transcription service, we send it to the text generation service
        /*this.components.speech2text?.on('data', (data: Buffer, identifier: string) => {
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
        });*/
        this.components.functionalityCoding?.on('data', (data: any) => {
            // @@todo here
            const codegenerationquery = JSON.parse(data.message.toString());
            const objectName = codegenerationquery.objectname;
            const description = codegenerationquery.data;
            
            console.log(objectName);
            console.log(description);
            let messageToSend = "The Unity game object to attach the script is a " + objectName + "." + description + ".";
            
            var threshold = 80; //for filtering useless responses
            if (messageToSend.length != 0 && messageToSend.length > threshold && this.isGenerating == false) {
                // Remove all newlines from the response
                messageToSend = messageToSend.replace(/(\r\n|\n|\r)/gm, '');

                if (messageToSend.trim()) {
                    const message = ('Query:: ' + messageToSend).trim();
                    this.log(message);

                    this.components.codeGenerationService?.sendToChildProcess('default', messageToSend + '\n');
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
                
                this.scene.send(99, {
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
    const app = new CodeGenerationUnderstandingAgent(absConfigPath);
    app.start();
}
