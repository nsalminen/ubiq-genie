import { ServiceController } from '../../components/service';
import { NetworkScene } from 'ubiq';
import nconf from 'nconf';

class CodeGenerationService extends ServiceController {
    constructor(scene: NetworkScene) {
        super(scene, 'CodeGenerationService');
        console.log('preprompt', nconf.get('preprompt'), 'prompt_suffix', nconf.get('prompt_suffix'));

        this.registerChildProcess('default', 'python', [
            '-u',
            '../../services/code_generation/openai_chatgpt.py',
            '--preprompt',
            nconf.get('preprompt') || '',
            '--prompt_suffix',
            nconf.get('prompt_suffix') || '',
        ]);
    }
}

export { CodeGenerationService };
