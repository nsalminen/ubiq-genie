import { ServiceController } from '../../components/service';
import { NetworkScene } from 'ubiq';
import nconf from 'nconf';

class EvaluationService extends ServiceController {
    constructor(scene: NetworkScene) {
        super(scene, 'EvaluationService');
        console.log('preprompt', nconf.get('preprompt'), 'prompt_suffix', nconf.get('prompt_suffix'));

        this.registerChildProcess('default', 'python', [
            '-u',
            '../../services/evaluation/openai_chatgpt.py',
            '--preprompt',
            nconf.get('evaluation_prompt') || '',
            '--prompt_suffix',
            nconf.get('evaluation_prompt_suffix') || '',
        ]);
    }
}

export { EvaluationService };
