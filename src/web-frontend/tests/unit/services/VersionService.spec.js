import VersionService, { versionService } from "@/services/VersionService";
import axios, { AxiosResponse } from "axios";
import VersionInfo from "@/models/VersionInfo";

jest.mock("axios");

afterEach(() => {
  jest.clearAllMocks();
});

describe("getVersion", () => {
    describe("when API call is made", () => {
      it("executes using the version url", async () => {
        axios.get.mockImplementationOnce(() =>
            Promise.resolve({ data: new VersionInfo("0.0.0") })
        );

        var baseUrl = "http://test-apotheca.com";
        process.env.VUE_APP_API_SERVER_URL = baseUrl;
        var vs = new VersionService();
        
        var expectedVersionUrl = `${baseUrl}${vs.versionEndPoint}`;
  
        await vs.getVersion();
        expect(axios.get).toHaveBeenCalledTimes(1);
        expect(axios.get).toHaveBeenCalledWith(expectedVersionUrl);
      });
    });
  
    describe("when API call is succcessful", () => {
      it("should return the version number", async () => {
        const mockVersionInfo = new VersionInfo("2.3.4");
  
        axios.get.mockImplementationOnce(() =>
          Promise.resolve({ data: mockVersionInfo })
        );
  
        const result = await versionService.getVersion();
  
        expect(axios.get).toHaveBeenCalledTimes(1);
        expect(result).toEqual(mockVersionInfo);
        expect(result.version).toEqual(mockVersionInfo.version);
      });
    });
  });
  