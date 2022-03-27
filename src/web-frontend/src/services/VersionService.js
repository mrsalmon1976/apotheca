import axios from 'axios';

export default class VersionService {

    versionEndPoint = "/version";
    
    async getVersion() {
        var url = `${process.env.VUE_APP_API_SERVER_URL}${this.versionEndPoint}`;
        var response = await axios.get(url)
        console.log(`Apotheca version number: ${response.data.version}`);
        return response.data;
      }
}

export const versionService = new VersionService();
