import { ServiceController } from '../../components/service';
import { NetworkScene } from 'ubiq';
import nconf from 'nconf';

class UnderstandingService extends ServiceController {
    constructor(scene: NetworkScene) {
        super(scene, 'UnderstandingService');
        console.log('preprompt', nconf.get('preprompt'), 'prompt_suffix', nconf.get('prompt_suffix'));

        this.registerChildProcess('default', 'python', [
            '-u',
            '../../services/understanding/openai_chatgpt.py',
            '--preprompt',
            nconf.get('functionprompt') || '',
            '--prompt_suffix',
            nconf.get('functionprompt_suffix') || '',
        ]);
    }
}

export { UnderstandingService };
